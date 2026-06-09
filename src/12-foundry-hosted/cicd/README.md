# Deploy the hosted agent without `azd` (Bicep + REST)

This folder deploys the Module 12 hosted agent to Azure AI Foundry using only
**Bicep** and the **Foundry data-plane REST API** — no `azd`. It's the path most
enterprises use, since `azd` is rarely the production CD mechanism.

The flow is identical whether you run it locally or in CI:

```
Bicep infra  ->  az acr build  ->  POST /agents/{name}/versions  ->  poll until active
```

By **default** the script deploys **into your existing Modules 1–11 Foundry
account/project**, so the hosted `trip-planner` sits **next to the prompt agent**
under **Agents**. `infra/main.shared.bicep` adds only the hosted-agent pieces (an
ACR + RBAC) — it doesn't touch your model or prompt agent. You can opt into a
fresh **standalone** account instead; see
[the standalone alternative](#alternative--standalone-fresh-account--resource-group).

## What gets deployed

**Default (existing target)** — `infra/main.shared.bicep` references your existing
account/project and adds just:

| Resource | Module | Why |
| --- | --- | --- |
| Container registry (ACR) | `modules/acr.bicep` | Stores the agent image |
| ACR connection on the project (`ManagedIdentity`) + AcrPull | `modules/acr.bicep` | Lets the runtime pull the image |
| Foundry User → project identity (on the account) | `main.shared.bicep` | Project-level model access |

**Standalone target** — `infra/main.bicep` (subscription scope) instead creates a
*fresh* resource group containing a whole new Foundry account + project + model +
ACR + Log Analytics/App Insights (the `modules/*.bicep` set).

Three RBAC grants matter and are handled for you in both targets:

- **AcrPull → project managed identity** (Bicep) — so the runtime can pull the image.
- **Foundry Project Manager → *you* (the deployer)** at the project scope — granted by
  `deploy.sh`, because the Foundry data plane evaluates `agents/write` at the
  **project** scope (Owner at the subscription is *not* enough to register a version).
- **Foundry User → the agent's instance identity** on the account — granted by
  `deploy.sh` *after* the version is active, so the running container can call the
  model. (Foundry v2 hosted agents run under a per-agent identity, not the project's.)

> No explicit `capabilityHost` resource is needed — the hosted-agent runtime is
> provisioned automatically by the `2026-03-01` account API. Your base account
> must already be on that API (the repo-root `infra/main.bicep` is — redeploy it
> once if your account predates the bump).

## Run it locally

The **[Module 12 README → Step 4](../README.md#step-4--deploy-to-foundry-bicep--rest-api)**
has the full sign-in → deploy → verify → iterate walkthrough. In short, from
`src/12-foundry-hosted`, set `ACCOUNT_NAME`/`PROJECT_NAME` to match your
[`../../../infra/main.bicepparam`](../../../infra/main.bicepparam) (account =
`<name>-foundry`):

```bash
RESOURCE_GROUP=rg-tripbot \
ACCOUNT_NAME=tripbot-foundry \
PROJECT_NAME=tripbot-project \
LOCATION=eastus2 \
./cicd/deploy.sh
```

> **One-time prerequisite.** Your base account must be on the **`2026-03-01`**
> API to host containers. The repo-root `infra/main.bicep` already is, so if your
> account predates that bump, redeploy it once (idempotent — project, model, and
> prompt agent are preserved):
> ```bash
> az deployment group create -g rg-tripbot \
>   --template-file infra/main.bicep --parameters infra/main.bicepparam
> ```

Prerequisites: `az` (logged in), `git`, `curl`, `python3`. You do **not** need
Docker — `az acr build` builds the image in Azure. Your account needs **Owner**
(or **Contributor + Role Based Access Control Administrator**) at the
subscription scope, because the script creates role assignments.

In order, the script runs: Bicep add-on → grant **Foundry Project Manager** to
you (+120s propagation) → `az acr build` → register the `trip-planner` version
via REST → poll until `active` → grant the agent identity **Foundry User**.

### Options

Useful flags:

- `--skip-infra` — skip the Bicep deploy and just rebuild the image + register a
  new agent version (fast inner loop after a code change).
- `--skip-rbac` — skip the Foundry Project Manager grant + 120s propagation wait
  (use when you already have the role on the project).

## Alternative — standalone fresh account / resource group

Want the hosted agent in a **completely separate** Foundry account and resource
group (leaving Modules 1–11 untouched)? Use the **standalone** target. From
`src/12-foundry-hosted`:

```bash
DEPLOY_TARGET=standalone \
ENVIRONMENT_NAME=tripbot-cicd \
LOCATION=eastus2 \
./cicd/deploy.sh
```

This runs `infra/main.bicep` at subscription scope, creating a brand-new resource
group (`rg-<ENVIRONMENT_NAME>`) with its own Foundry account + project + model +
ACR. Edit the model / region / project-name defaults in
[`infra/main.parameters.json`](infra/main.parameters.json).

Tear it down when finished:

```bash
az group delete -n rg-tripbot-cicd --yes --no-wait
```

> If the first version reports `failed` with an image-pull or runtime error,
> RBAC (or a freshly-enabled hosting environment) likely hadn't propagated yet —
> re-run with `--skip-infra` to register a new version against the same infra.

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
