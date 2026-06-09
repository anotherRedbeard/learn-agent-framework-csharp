#!/usr/bin/env bash
# deploy.sh — deploy the Module 12 hosted agent to Azure AI Foundry WITHOUT azd.
#
# What it does:
#   1. Deploys infra (Foundry account + project + model + ACR) via Bicep.
#   2. Grants the caller "Foundry Project Manager" at the project scope so the
#      Foundry data plane will accept the agent-version create call.
#   3. Builds + pushes the agent image with `az acr build` (no local Docker).
#   4. Registers a new hosted-agent version via the Foundry data-plane REST API.
#   5. Polls until the version is `active`.
#   6. Grants the agent's per-agent "instance identity" Foundry User on the
#      account so the running container can call the model.
#
# Prerequisites:
#   - az login  (a user, OR a service principal already logged in for CI)
#   - The caller has Owner (or Contributor + Role Based Access Control
#     Administrator) at the subscription scope — needed because the Bicep
#     creates a resource group and role assignments.
#   - git, curl, python3
#
# Usage:
#   First deploy:        ./cicd/deploy.sh
#   Code change only:    ./cicd/deploy.sh --skip-infra
#   Skip the RBAC grant: ./cicd/deploy.sh --skip-rbac   (if access is pre-granted)
#
# By DEFAULT this deploys INTO your existing Modules 1-11 Foundry account/project
# (so the hosted agent sits next to the prompt agent). It reads:
#   RESOURCE_GROUP    (default: rg-tripbot)
#   ACCOUNT_NAME      (default: tripbot-foundry)   — your <name>-foundry account
#   PROJECT_NAME      (default: tripbot-project)
#   LOCATION          (default: eastus2)
#   AGENT_NAME        (default: trip-planner)
#   (Set ACCOUNT_NAME/PROJECT_NAME to match your infra/main.bicepparam. The
#    repo-root infra/main.bicep must already be on the 2026-03-01 account API.)
#
# To create a COMPLETELY SEPARATE standalone account + resource group instead:
#   DEPLOY_TARGET=standalone ./cicd/deploy.sh
#   (override ENVIRONMENT_NAME, default: tripbot-cicd, to name the new stack)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODULE_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"   # src/12-foundry-hosted — the image build context
INFRA_DIR="${SCRIPT_DIR}/infra"

# ── Configuration (override via env) ─────────────────────────────────────────
ENVIRONMENT_NAME="${ENVIRONMENT_NAME:-tripbot-cicd}"
LOCATION="${LOCATION:-eastus2}"
AGENT_NAME="${AGENT_NAME:-trip-planner}"
IMAGE_NAME="${IMAGE_NAME:-trip-planner}"
DEPLOYMENT_NAME="deploy-${ENVIRONMENT_NAME}"
AGENT_CPU="${AGENT_CPU:-0.25}"
AGENT_MEMORY="${AGENT_MEMORY:-0.5Gi}"
API_VERSION="2025-11-15-preview"
MAX_POLL_SECONDS="${MAX_POLL_SECONDS:-600}"

# existing (default) reuses your Modules 1-11 Foundry account/project so the
# hosted agent sits next to the prompt agent; standalone creates a fresh account.
DEPLOY_TARGET="${DEPLOY_TARGET:-existing}"
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-tripbot}"
ACCOUNT_NAME="${ACCOUNT_NAME:-tripbot-foundry}"
PROJECT_NAME_PARAM="${PROJECT_NAME:-tripbot-project}"

# ── Argument parsing ─────────────────────────────────────────────────────────
SKIP_INFRA=false
SKIP_RBAC="${SKIP_RBAC:-false}"
for arg in "$@"; do
  case $arg in
    --skip-infra) SKIP_INFRA=true ;;
    --skip-rbac)  SKIP_RBAC=true ;;
    *) echo "Unknown argument: $arg" >&2; exit 1 ;;
  esac
done

# Reject typos so a misspelled target never silently falls back to standalone.
case "$DEPLOY_TARGET" in
  standalone|existing) ;;
  *) echo "ERROR: DEPLOY_TARGET must be 'standalone' or 'existing' (got '${DEPLOY_TARGET}')." >&2; exit 1 ;;
esac

if [ "$DEPLOY_TARGET" = "existing" ]; then
  echo "==> Target: EXISTING project '${PROJECT_NAME_PARAM}' on account '${ACCOUNT_NAME}' (resource group ${RESOURCE_GROUP})."
  echo "    Subscription: $(az account show --query name -o tsv 2>/dev/null || echo '?')"
  echo "    NOTE: the repo-root infra/main.bicep must already be redeployed on the"
  echo "          2026-03-01 account API, or hosted-agent activation will fail."
fi

# ── Step 1: Deploy infrastructure ────────────────────────────────────────────
if [ "$SKIP_INFRA" = false ]; then
  if [ "$DEPLOY_TARGET" = "existing" ]; then
    echo "==> Deploying hosted-agent add-on into existing project '${PROJECT_NAME_PARAM}' (resource group ${RESOURCE_GROUP})..."
    az deployment group create \
      --resource-group "${RESOURCE_GROUP}" \
      --name           "${DEPLOYMENT_NAME}" \
      --template-file  "${INFRA_DIR}/main.shared.bicep" \
      --parameters     location="${LOCATION}" existingAccountName="${ACCOUNT_NAME}" existingProjectName="${PROJECT_NAME_PARAM}" \
      --output none

    DEPLOY_STATE=$(az deployment group show --resource-group "${RESOURCE_GROUP}" --name "${DEPLOYMENT_NAME}" --query properties.provisioningState -o tsv)
  else
    echo "==> Deploying standalone infrastructure (Bicep)..."
    az deployment sub create \
      --name          "${DEPLOYMENT_NAME}" \
      --location      "${LOCATION}" \
      --template-file "${INFRA_DIR}/main.bicep" \
      --parameters    "@${INFRA_DIR}/main.parameters.json" \
      --parameters    environmentName="${ENVIRONMENT_NAME}" location="${LOCATION}" aiDeploymentsLocation="${LOCATION}" \
      --output none

    DEPLOY_STATE=$(az deployment sub show --name "${DEPLOYMENT_NAME}" --query properties.provisioningState -o tsv)
  fi

  if [ "${DEPLOY_STATE}" != "Succeeded" ]; then
    echo "ERROR: Deployment finished in state '${DEPLOY_STATE}' — not Succeeded." >&2
    exit 1
  fi
  echo "    Infrastructure deployed."
else
  echo "==> Skipping infrastructure deployment (--skip-infra)."
fi

# ── Step 2: Read deployment outputs ──────────────────────────────────────────
echo "==> Reading deployment outputs..."
if [ "$DEPLOY_TARGET" = "existing" ]; then
  OUTPUTS=$(az deployment group show --resource-group "${RESOURCE_GROUP}" --name "${DEPLOYMENT_NAME}" --query properties.outputs -o json)
else
  OUTPUTS=$(az deployment sub show --name "${DEPLOYMENT_NAME}" --query properties.outputs -o json)
fi
if [ -z "${OUTPUTS}" ] || [ "${OUTPUTS}" = "null" ] || [ "${OUTPUTS}" = "{}" ]; then
  echo "ERROR: Deployment '${DEPLOYMENT_NAME}' returned no outputs." >&2
  exit 1
fi
_get() { echo "$OUTPUTS" | python3 -c "import sys,json; d={k.upper():v for k,v in json.load(sys.stdin).items()}; print(d['$1']['value'])"; }

PROJECT_ID=$(            _get AZURE_AI_PROJECT_ID)
PROJECT_NAME=$(          _get AZURE_AI_PROJECT_NAME)
PROJECT_ENDPOINT=$(      _get AZURE_AI_PROJECT_ENDPOINT)
ACR_ENDPOINT=$(          _get AZURE_CONTAINER_REGISTRY_ENDPOINT)
MODEL_DEPLOYMENT_NAME=$( _get AZURE_AI_MODEL_DEPLOYMENT_NAME)
ACR_NAME="${ACR_ENDPOINT%%.azurecr.io}"

# Guard against reading a stale deployment of the same name (e.g. with
# --skip-infra after switching targets): the outputs must name the project we
# were asked to target.
if [ "$DEPLOY_TARGET" = "existing" ] && [ "${PROJECT_NAME}" != "${PROJECT_NAME_PARAM}" ]; then
  echo "ERROR: deployment '${DEPLOYMENT_NAME}' outputs project '${PROJECT_NAME}', not the requested '${PROJECT_NAME_PARAM}'." >&2
  echo "       Those outputs are stale — re-run without --skip-infra to redeploy." >&2
  exit 1
fi

echo "    Project         : ${PROJECT_NAME}"
echo "    Project endpoint: ${PROJECT_ENDPOINT}"
echo "    ACR             : ${ACR_ENDPOINT}"
echo "    Model deployment: ${MODEL_DEPLOYMENT_NAME}"

# ── Step 3: Grant Foundry Project Manager at the project scope ────────────────
# The Foundry data plane evaluates 'agents/write' at the project scope — not at
# subscription/RG scope — so the caller needs this role on the project itself.
# Idempotent: a no-op if already assigned.
if [ "$SKIP_RBAC" = true ]; then
  echo "==> Skipping RBAC grant (--skip-rbac)."
else
  echo "==> Granting Foundry Project Manager at project scope..."
  if [ -n "${DEPLOYER_OBJECT_ID:-}" ]; then
    # Explicit object id (best for CI — no directory read needed).
    PRINCIPAL_ID="${DEPLOYER_OBJECT_ID}"
    PRINCIPAL_TYPE="${DEPLOYER_PRINCIPAL_TYPE:-ServicePrincipal}"
  else
    PRINCIPAL_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || true)
    PRINCIPAL_TYPE="User"
    if [ -z "${PRINCIPAL_ID}" ]; then
      # Running as a service principal — resolve its object id from the app id.
      # This needs Microsoft Graph read; in CI prefer setting DEPLOYER_OBJECT_ID.
      APP_ID=$(az account show --query user.name -o tsv)
      PRINCIPAL_ID=$(az ad sp show --id "${APP_ID}" --query id -o tsv 2>/dev/null || true)
      PRINCIPAL_TYPE="ServicePrincipal"
      if [ -z "${PRINCIPAL_ID}" ]; then
        echo "ERROR: could not resolve the deployer's object id for app '${APP_ID}'." >&2
        echo "       Set DEPLOYER_OBJECT_ID to the service principal object id" >&2
        echo "       (in CI, the AZURE_CLIENT_OBJECT_ID secret) and re-run." >&2
        exit 1
      fi
    fi
  fi
  ROLE_FOUNDRY_PM="eadc314b-1a2d-4efa-be10-5d325db5065e"  # Foundry Project Manager

  # Idempotent: create only if the assignment is missing, and surface real errors.
  EXISTING=$(az role assignment list --assignee "${PRINCIPAL_ID}" --role "${ROLE_FOUNDRY_PM}" --scope "${PROJECT_ID}" --query "[].id" -o tsv 2>/dev/null || true)
  if [ -n "${EXISTING}" ]; then
    echo "    Role already assigned."
  else
    az role assignment create \
      --role "${ROLE_FOUNDRY_PM}" \
      --assignee-object-id "${PRINCIPAL_ID}" \
      --assignee-principal-type "${PRINCIPAL_TYPE}" \
      --scope "${PROJECT_ID}" \
      --output none
    echo "    Role assigned."
  fi
  echo "    Waiting 120s for RBAC propagation..."
  sleep 120
fi

# ── Step 4: Build + push the image with az acr build (no local Docker) ────────
IMAGE_TAG=$(git -C "${MODULE_DIR}" rev-parse --short HEAD 2>/dev/null || date -u +%Y%m%d%H%M%S)
FULL_IMAGE="${ACR_ENDPOINT}/${IMAGE_NAME}:${IMAGE_TAG}"
echo "==> Building + pushing image ${FULL_IMAGE} (az acr build)..."
az acr build \
  --registry "${ACR_NAME}" \
  --image    "${IMAGE_NAME}:${IMAGE_TAG}" \
  --file     "${MODULE_DIR}/Dockerfile" \
  "${MODULE_DIR}" \
  --output none
echo "    Image pushed."

# ── Step 5: Register a hosted-agent version (Foundry data plane) ──────────────
echo "==> Registering hosted-agent version..."
FOUNDRY_TOKEN=$(az account get-access-token --resource "https://ai.azure.com/" --query accessToken -o tsv)

AGENT_REQUEST_BODY=$(python3 - <<EOF
import json
print(json.dumps({
  "definition": {
    "kind": "hosted",
    "container_protocol_versions": [{"protocol": "responses", "version": "1.0.0"}],
    "cpu": "${AGENT_CPU}",
    "memory": "${AGENT_MEMORY}",
    "environment_variables": {"AZURE_AI_MODEL_DEPLOYMENT_NAME": "${MODEL_DEPLOYMENT_NAME}"},
    "image": "${FULL_IMAGE}"
  }
}))
EOF
)

RESPONSE=$(curl -sS -X POST \
  "${PROJECT_ENDPOINT}/agents/${AGENT_NAME}/versions?api-version=${API_VERSION}" \
  -H "Authorization: Bearer ${FOUNDRY_TOKEN}" \
  -H "Content-Type: application/json" \
  -d "${AGENT_REQUEST_BODY}" \
  -w $'\n__HTTP_STATUS__%{http_code}')
HTTP_STATUS=$(echo "${RESPONSE}" | sed -n 's/^__HTTP_STATUS__//p')
BODY=$(echo "${RESPONSE}" | sed '/^__HTTP_STATUS__/d')
if [ "${HTTP_STATUS}" -lt 200 ] || [ "${HTTP_STATUS}" -ge 300 ]; then
  echo "ERROR: agent-version POST returned HTTP ${HTTP_STATUS}" >&2
  echo "${BODY}" >&2
  exit 1
fi
AGENT_VERSION=$(echo "${BODY}" | python3 -c "import sys,json; print(json.load(sys.stdin)['version'])")
echo "    Agent ${AGENT_NAME} version ${AGENT_VERSION} created."

# ── Step 6: Poll until the version is active ─────────────────────────────────
echo "==> Polling until version ${AGENT_VERSION} is active (timeout ${MAX_POLL_SECONDS}s)..."
DEADLINE=$(( $(date +%s) + MAX_POLL_SECONDS ))
while :; do
  STATUS_BODY=$(curl -s -f \
    "${PROJECT_ENDPOINT}/agents/${AGENT_NAME}/versions/${AGENT_VERSION}?api-version=${API_VERSION}" \
    -H "Authorization: Bearer ${FOUNDRY_TOKEN}")
  STATUS=$(echo "${STATUS_BODY}" | python3 -c "import sys,json; print(json.load(sys.stdin).get('status',''))")
  echo "      status=${STATUS}"
  case "${STATUS}" in
    active) break ;;
    failed)
      echo "ERROR: version build failed." >&2
      echo "${STATUS_BODY}" >&2
      exit 1 ;;
  esac
  if [ "$(date +%s)" -ge "${DEADLINE}" ]; then
    echo "ERROR: version did not become active within ${MAX_POLL_SECONDS}s." >&2
    exit 1
  fi
  sleep 10
done

# ── Step 7: Grant the agent's instance identity model access ──────────────────
# Foundry v2 hosted agents do NOT run as the project managed identity — each
# agent gets its own "instance identity" (a per-agent managed identity created
# when the version is built). That principal — not the project — is what calls
# the model, so it needs Foundry User on the account or every invoke returns
# HTTP 401 (.../OpenAI/responses/write). The identity exists only after the
# version is active, so we resolve and grant it here. Re-runs are idempotent and
# a new version's identity (e.g. after --skip-infra) is picked up automatically.
if [ "$SKIP_RBAC" = true ]; then
  echo "==> Skipping agent-identity RBAC grant (--skip-rbac)."
else
  ACCOUNT_ID="${PROJECT_ID%/projects/*}"
  if [ "${ACCOUNT_ID}" = "${PROJECT_ID}" ] || [ -z "${ACCOUNT_ID}" ]; then
    echo "ERROR: could not derive the account id from project id '${PROJECT_ID}'." >&2
    exit 1
  fi

  # Foundry can take a moment to project the instance identity after the version
  # reports active, so poll for it instead of trusting a single GET.
  echo "==> Resolving the agent instance identity..."
  AGENT_PRINCIPAL_ID=""
  IDENTITY_DEADLINE=$(( $(date +%s) + 120 ))
  while :; do
    AGENT_BODY=$(curl -s \
      "${PROJECT_ENDPOINT}/agents/${AGENT_NAME}?api-version=${API_VERSION}" \
      -H "Authorization: Bearer ${FOUNDRY_TOKEN}" \
      -H "Foundry-Features: HostedAgents=V1Preview" \
      -w $'\n__HTTP_STATUS__%{http_code}' || true)
    IDENTITY_STATUS=$(echo "${AGENT_BODY}" | sed -n 's/^__HTTP_STATUS__//p')
    IDENTITY_JSON=$(echo "${AGENT_BODY}" | sed '/^__HTTP_STATUS__/d')
    AGENT_PRINCIPAL_ID=$(echo "${IDENTITY_JSON}" | python3 -c "
import sys, json
def pid(o):
    if not isinstance(o, dict):
        return ''
    ii = o.get('instance_identity')
    return ii.get('principal_id', '') if isinstance(ii, dict) else ''
try:
    d = json.load(sys.stdin)
except Exception:
    print(''); sys.exit()
out = ''
if isinstance(d, dict):
    v = d.get('versions')
    if isinstance(v, dict):
        out = pid(v.get('latest'))
    if not out:
        out = pid(d)
print(out)
" 2>/dev/null || true)
    [ -n "${AGENT_PRINCIPAL_ID}" ] && break
    if [ "$(date +%s)" -ge "${IDENTITY_DEADLINE}" ]; then break; fi
    sleep 10
  done

  if [ -z "${AGENT_PRINCIPAL_ID}" ]; then
    echo "WARNING: could not resolve the agent instance identity (last HTTP ${IDENTITY_STATUS:-?})." >&2
    echo "         The agent will return HTTP 401 on invoke until you grant it manually:" >&2
    echo "         az role assignment create --role 53ca6127-db72-4b80-b1b0-d745d6d5456d \\" >&2
    echo "           --assignee-object-id <agent-principal-id> --assignee-principal-type ServicePrincipal \\" >&2
    echo "           --scope ${ACCOUNT_ID}" >&2
    echo "         (find <agent-principal-id> in the agent JSON under instance_identity.principal_id)" >&2
  else
    ROLE_FOUNDRY_USER="53ca6127-db72-4b80-b1b0-d745d6d5456d"  # Foundry User
    EXISTING_USER=$(az role assignment list --assignee "${AGENT_PRINCIPAL_ID}" --role "${ROLE_FOUNDRY_USER}" --scope "${ACCOUNT_ID}" --query "[].id" -o tsv 2>/dev/null || true)
    if [ -n "${EXISTING_USER}" ]; then
      echo "==> Agent instance identity already has Foundry User on the account."
    else
      echo "==> Granting Foundry User to the agent instance identity at account scope..."
      if ! az role assignment create \
        --role "${ROLE_FOUNDRY_USER}" \
        --assignee-object-id "${AGENT_PRINCIPAL_ID}" \
        --assignee-principal-type ServicePrincipal \
        --scope "${ACCOUNT_ID}" \
        --output none; then
        echo "ERROR: failed to grant Foundry User to the agent identity at:" >&2
        echo "         ${ACCOUNT_ID}" >&2
        echo "       You need Owner or Role Based Access Control Administrator on that" >&2
        echo "       account. Re-run with --skip-rbac after granting it, or run the grant" >&2
        echo "       manually for principal ${AGENT_PRINCIPAL_ID}." >&2
        exit 1
      fi
      echo "    Granted. Allow 5-15 min for data-plane RBAC to propagate before the first invoke."
    fi
  fi
fi

echo ""
echo "Done. Agent '${AGENT_NAME}' (v${AGENT_VERSION}) is active on project '${PROJECT_NAME}'."
echo "Test it in the Foundry portal (https://ai.azure.com/) or via REST:"
echo "  POST ${PROJECT_ENDPOINT}/agents/${AGENT_NAME}/endpoint/protocols/openai/responses?api-version=${API_VERSION}"
