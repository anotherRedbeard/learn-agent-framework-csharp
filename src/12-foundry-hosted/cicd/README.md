# Deploy the hosted agent without `azd` (Bicep + REST)

This folder deploys the Module 12 hosted agent to Azure AI Foundry using only
**Bicep** and the **Foundry data-plane REST API** — no `azd`. It's the path most
enterprises use, since `azd` is rarely the production CD mechanism.

The flow is identical whether you run it locally or in CI:

```
Bicep infra  ->  az acr build  ->  POST /agents/{name}/versions  ->  poll until active
```

## What gets deployed

`infra/main.bicep` (subscription scope) creates a resource group and, inside it:

| Resource | Module | Why |
| --- | --- | --- |
| Azure AI Foundry account (`AIServices`) + model deployment | `modules/foundry.bicep` | Hosts the project and the model the agent calls |
| Foundry project (system-assigned identity) | `modules/foundry-project.bicep` | The identity the agent container runs as |
| Container registry (ACR) | `modules/acr.bicep` | Stores the agent image |
| ACR connection on the project (`ManagedIdentity`) + AcrPull | `modules/acr.bicep` | Lets the runtime pull the image |
| Log Analytics + Application Insights + project connection | `modules/loganalytics.bicep`, `modules/applicationinsights.bicep`, `modules/foundry-project.bicep` | Telemetry for the agent (used by later observability modules) |

Two RBAC grants matter and are handled for you:

- **AcrPull → project managed identity** (in Bicep) — so the runtime can pull the image.
- **Foundry User → project managed identity** (in Bicep) — so the container can call the model endpoint.
- **Foundry Project Manager → *you* (the deployer)** at the project scope — granted by
  `deploy.sh`, because the Foundry data plane evaluates `agents/write` at the
  **project** scope, not subscription/RG. (Owner at the subscription is *not*
  enough to register an agent version.)

> No explicit `capabilityHost` resource is needed — the hosted-agent runtime is
> provisioned automatically by the account API version used here.

## Run it locally (test from scratch)

Prerequisites: `az` (logged in), `git`, `curl`, `python3`. You do **not** need
Docker — `az acr build` builds the image in Azure. Your account needs **Owner**
(or **Contributor + Role Based Access Control Administrator**) at the
subscription scope, because the Bicep creates a resource group *and* role
assignments.

**1. Sign in and select the subscription**

```bash
az login
az account set --subscription <your-subscription-id>
az account show --query "{sub:name, user:user.name}" -o table
```

**2. Deploy**

```bash
cd src/12-foundry-hosted

# A fresh ENVIRONMENT_NAME keeps these resources separate from any existing
# tripbot project. It creates resource group rg-<ENVIRONMENT_NAME>.
ENVIRONMENT_NAME=tripbot-cicd LOCATION=eastus2 ./cicd/deploy.sh
```

In order, this: deploys infra (Foundry account + project + model + ACR + App
Insights) → grants you **Foundry Project Manager** on the project (+120s
propagation wait) → builds/pushes the image with `az acr build` → registers the
`trip-planner` agent version → polls until `active`. The first run takes
~5–10 min; it prints the project endpoint and an invoke URL when done.

**3. Verify it works**

```bash
PROJECT_ENDPOINT="<printed by the script>"   # https://<acct>.services.ai.azure.com/api/projects/trip-project
TOKEN=$(az account get-access-token --resource https://ai.azure.com/ --query accessToken -o tsv)

curl -sS -X POST \
  "$PROJECT_ENDPOINT/agents/trip-planner/endpoint/protocols/openai/responses?api-version=2025-11-15-preview" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"input":"Plan a 3-day trip to Tokyo in cherry blossom season.","store":true}'
```

Or open the [Foundry portal](https://ai.azure.com/), select project
`trip-project`, and test `trip-planner` under **Agents**.

**4. Iterate on a code change (fast inner loop)**

Re-register a new version without re-running the Bicep:

```bash
ENVIRONMENT_NAME=tripbot-cicd LOCATION=eastus2 ./cicd/deploy.sh --skip-infra
```

**5. Tear down when finished**

```bash
az group delete -n rg-tripbot-cicd --yes --no-wait
```

### Options

Override the defaults with env vars:

```bash
ENVIRONMENT_NAME=tripbot-cicd LOCATION=eastus2 AGENT_NAME=trip-planner ./cicd/deploy.sh
```

Useful flags:

- `--skip-infra` — skip the Bicep deploy and just rebuild the image + register a
  new agent version (fast inner loop after a code change).
- `--skip-rbac` — skip the Foundry Project Manager grant + 120s propagation wait
  (use when you already have the role on the project).

Edit the model / region / project name defaults in
[`infra/main.parameters.json`](infra/main.parameters.json).

## Run it in CI (GitHub Actions)

The workflow [`.github/workflows/deploy-hosted-agent.yml`](../../../.github/workflows/deploy-hosted-agent.yml)
(at the repo root) logs in with OIDC and runs the same `deploy.sh`.

1. Create an Entra app registration with a **federated credential** for this repo
   and grant it **Owner** (or **Contributor + RBAC Administrator**) at the
   subscription scope.
2. Add these repository secrets:
   - `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
   - `AZURE_CLIENT_OBJECT_ID` *(recommended)* — the **object id** of that service
     principal. The script grants Foundry Project Manager to this object id
     directly, avoiding a Microsoft Graph read the SP may not be allowed to make.
3. Run the workflow from the **Actions** tab (`workflow_dispatch`), choosing the
   environment name, region, and agent name.

## Test the deployed agent

After the script reports the version is `active`, call it (a fresh Foundry token
is required — `az account get-access-token --resource https://ai.azure.com/`):

```bash
PROJECT_ENDPOINT="https://<account>.services.ai.azure.com/api/projects/<project>"
TOKEN=$(az account get-access-token --resource https://ai.azure.com/ --query accessToken -o tsv)

curl -sS -X POST \
  "$PROJECT_ENDPOINT/agents/trip-planner/endpoint/protocols/openai/responses?api-version=2025-11-15-preview" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"input":"Plan a 3-day trip to Tokyo in cherry blossom season.","store":true}'
```

Or open the [Foundry portal](https://ai.azure.com/), select your project, and
test the agent under **Agents**.

## First-deploy timing

A brand-new account/project takes a few minutes to provision the hosting
environment before the first agent version can become `active` — `deploy.sh`
polls for up to 10 minutes (override with `MAX_POLL_SECONDS`). Subsequent
versions are fast because the hosting environment is already warm.

If the very first version reports `failed` with an image-pull/ACR auth error,
the project's AcrPull grant likely hadn't propagated yet — just re-run
`./cicd/deploy.sh --skip-infra` to register a new version against the same
infrastructure. (The script already waits 120s after the RBAC grant and the
`az acr build` step adds several more minutes, so this is rare.)
