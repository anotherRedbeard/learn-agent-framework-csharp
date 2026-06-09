# Deploy the hosted agent without `azd` (Bicep + REST)

This folder deploys the Module 12 hosted agent to Azure AI Foundry using only
**Bicep** and the **Foundry data-plane REST API** — no `azd`. It's the path most
enterprises use, since `azd` is rarely the production CD mechanism.

The flow is identical whether you run it locally or in CI:

```
Bicep infra  ->  az acr build  ->  POST /agents/{name}/versions  ->  poll until active
```

There are **two targets**:

- **Standalone** (default) — `infra/main.bicep` creates a *fresh* Foundry
  account + project just for the hosted agent. Self-contained, nothing else to
  set up.
- **Existing** — `infra/main.shared.bicep` adds only the hosted-agent pieces to
  the Foundry account/project the rest of the repo already uses
  (`tripbot-foundry` / `tripbot-project`), so the hosted agent sits **next to the
  Modules 1–11 prompt agent**. See
  [Deploy into an existing Foundry](#deploy-into-an-existing-foundry-side-by-side-with-modules-111).

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

## Run it locally

The **[Module 12 README → Step 4](../README.md#step-4--deploy-to-foundry-bicep--rest-api)**
has the full sign-in → deploy → verify → iterate walkthrough. In short, from
`src/12-foundry-hosted`:

```bash
ENVIRONMENT_NAME=tripbot-cicd LOCATION=eastus2 ./cicd/deploy.sh
```

Prerequisites: `az` (logged in), `git`, `curl`, `python3`. You do **not** need
Docker — `az acr build` builds the image in Azure. Your account needs **Owner**
(or **Contributor + Role Based Access Control Administrator**) at the
subscription scope, because the Bicep creates a resource group *and* role
assignments.

In order, the script runs: Bicep infra → grant **Foundry Project Manager** to
you (+120s propagation) → `az acr build` → register the `trip-planner` version
via REST → poll until `active`.

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

Tear down everything when finished:

```bash
az group delete -n rg-tripbot-cicd --yes --no-wait
```

## Deploy into an existing Foundry (side by side with Modules 1–11)

Want the hosted `trip-planner` to live in the **same** project as the prompt
agent from the earlier modules? Use the **existing** target instead of creating
a standalone account.

**Step 1 — make the base account hosting-capable (one time).** The hosted-agent
runtime is auto-provisioned only when the account is declared with the
**`2026-03-01`** account API. The repo-root [`infra/main.bicep`](../../../infra/main.bicep)
now uses that version, so redeploy it once (this is an idempotent in-place
update — your project, model, and prompt agent are preserved):

```bash
az deployment group create \
  -g rg-tripbot \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam
```

**Step 2 — add the hosted-agent pieces + register the agent.** From
`src/12-foundry-hosted`. `ACCOUNT_NAME` and `PROJECT_NAME` are **not** hardcoded
— they're env vars. Set them to **your** base resources: the account is
`<name>-foundry` where `<name>` and `projectName` are whatever you put in
[`infra/main.bicepparam`](../../../infra/main.bicepparam) (the example below uses
the repo defaults `name=tripbot` / `projectName=tripbot-project`).

```bash
DEPLOY_TARGET=existing \
RESOURCE_GROUP=rg-tripbot \
ACCOUNT_NAME=tripbot-foundry \
PROJECT_NAME=tripbot-project \
LOCATION=eastus2 \
./cicd/deploy.sh
```

[`infra/main.shared.bicep`](infra/main.shared.bicep) references the existing
account/project and adds only an **ACR** (+ AcrPull → project identity + ACR
connection) and **Foundry User → project identity** — nothing that would disturb
Modules 1–11. The script then builds the image and registers `trip-planner`
against `tripbot-project`, so both agents show up together under **Agents** in
the portal.

> If the first version reports `failed` with an image-pull or runtime error,
> the project's new RBAC (or the freshly-enabled hosting environment) likely
> hadn't propagated yet — re-run with `--skip-infra` to register a new version
> against the same infrastructure.

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
