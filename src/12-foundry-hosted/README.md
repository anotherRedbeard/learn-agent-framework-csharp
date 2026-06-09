# Module 12 — Foundry-hosted Container Agent

**Concept:** Take the same agent you hosted yourself in **Module 10** and lift
it into **Foundry-managed hosting** as a container, so the platform runs it for
you and clients call it through the OpenAI **Responses protocol**.

> ℹ️ This is the heaviest module in the repo — you'll publish a container image
> and register it with Foundry. The pattern itself is small (~30 lines of code);
> the operational pieces (Docker, registry, deploy) are what take real time.

## What you'll learn

- The difference between **self-hosted A2A** (Module 10) and **Foundry-hosted
  Responses** (this module) — same `AIAgent`, different host
- How `AddFoundryResponses(agent)` + `MapFoundryResponses()` replace
  `AddAIAgent()` + `MapHttpA2A()` when Foundry runs the container
- Why a Foundry-hosted agent is **one container per agent** (vs. Module 10's
  three agents in one process)
- What `agent.manifest.yaml` declares (`template.kind`, `protocols`, `resources`,
  `environment_variables`) and how Foundry uses it to provision the platform
  agent version
- The full local-loop: `dotnet run` → `docker run` → `./cicd/deploy.sh`

## When to use this pattern

Foundry-hosted containers are the right choice when:

- You want **Microsoft to operate** the runtime (no App Service, ACA, or AKS to
  babysit)
- The agent must be **callable from Foundry-aware clients** (Foundry portal,
  other Foundry agents, the Responses SDK)
- You want **managed identity, scaling, and routing** handled by the platform
- The agent fits the **Responses protocol** (not A2A — Foundry hosted agents do
  not speak A2A today)

Stay with Module 10's self-hosted A2A model when you need full control over the
process, the transport, or you have an existing A2A client ecosystem.

---

## Prerequisites

In addition to the [repo prerequisites](../../docs/prerequisites.md), you'll need:

- **Azure CLI** logged in (`az login`) with **Owner** — or **Contributor + Role
  Based Access Control Administrator** — at the **subscription** scope. The
  deploy script creates a resource group *and* role assignments.
- **`python3`** — the deploy script uses it to parse the REST responses.
- An Azure subscription with quota for a `gpt-4o-mini` deployment. The script
  provisions a **fresh, standalone** Foundry account, so you do **not** need an
  existing project (Modules 1–11 are untouched).
- **Docker** (or Podman) — *optional*, only for the local container loop in
  Step 3. The deploy path builds the image remotely with `az acr build`, so no
  local Docker daemon is required to ship.
- **Azure Developer CLI** ≥ **1.23.0** — *optional*, only if you use the `azd`
  alternative at the end of Step 4 (`azd ext install azure.ai.agents`).

---

## Step 1 — What changed vs Module 10

Module 10 hosted three agents (weather, travel, trip-planning workflow) in one
ASP.NET Core process exposed over **A2A**:

```csharp
// Module 10
builder.AddAIAgent("travel", instructions: "...", chatClientServiceKey: "chat-model")
    .AddA2AServer();
app.MapHttpA2A(server, card, "/a2a/travel");
```

Module 12 lifts **the travel agent** into a Foundry-managed container that
speaks the OpenAI **Responses protocol**:

```csharp
// Module 12
AIAgent agent = new AIProjectClient(endpoint, credential)
    .AsAIAgent(model, instructions, name, description);

var builder = AgentHost.CreateBuilder(args);
builder.Services.AddFoundryResponses(agent);
builder.RegisterProtocol("responses", e => e.MapFoundryResponses());
builder.Build().Run();
```

Three things to notice:

1. **Same `AIAgent` abstraction.** The persona, instructions, and description
   are identical to Module 10's travel agent — we changed only the host.
2. **One container per agent.** Foundry hosted agents are 1:1 with containers,
   so the weather agent and the trip-planning workflow stay in Module 10. (See
   the Stretch challenge to lift weather into its own container.)
3. **Different transport.** Clients no longer hit `/a2a/...` with A2A messages;
   they hit `POST /responses` with the OpenAI Responses payload shape.

---

## Step 2 — Run it locally with `dotnet run`

Assuming you exported `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_DEPLOYMENT_NAME`
per [`docs/prerequisites.md`](../../docs/prerequisites.md):

```bash
cd src/12-foundry-hosted
dotnet run
```

The agent starts on `http://localhost:8088`. Open `requests.http` and send the
first request. You should get back a JSON envelope shaped like the OpenAI
Responses API.

> ⚠️ **Local RBAC — you need Foundry User at the *account* level.** On `dotnet
> run` the agent calls the model as **your `az login` identity** (via
> `DefaultAzureCredential`). Module 12 uses the **Responses API**, which is
> evaluated at the **Foundry account** scope — not the project scope. If your
> user only has **Foundry User** on the *project* (the default the base
> `infra/main.bicep` grants), the call fails with **`HTTP 401 … lacks the
> required data action …/OpenAI/responses/write`**. Grant Foundry User at the
> **account** level once (it inherits down to the project, so Modules 1–11 keep
> working):
>
> ```bash
> ACCOUNT_ID=$(az cognitiveservices account show -g <your-rg> -n <name>-foundry --query id -o tsv)
> az role assignment create \
>   --assignee "$(az ad signed-in-user show --query id -o tsv)" \
>   --role "53ca6127-db72-4b80-b1b0-d745d6d5456d" \
>   --scope "$ACCOUNT_ID"
> ```
>
> Wait 5–15 min for data-plane RBAC to propagate, then retry. (Role ID
> `53ca6127-…` = **Foundry User**, formerly *Azure AI User*.)

> **Note on config sources.** Unlike most modules in this repo, Module 12 reads
> from `Environment.GetEnvironmentVariable` directly (not `IConfiguration`)
> because `AgentHost.CreateBuilder` is purpose-built for hosted-agent
> containers and Foundry only injects values via OS environment variables.
> That means **`dotnet user-secrets` is not picked up here** — use exported
> shell env vars (the prerequisites flow) or a `.env` file loaded by your shell.

> **Note on `AGENT_NAME`.** When Foundry runs the container in the cloud it
> injects `AGENT_NAME` so the same image can back multiple versions. Locally
> we default to `trip-planner` — the same value that lives in
> `agent.manifest.yaml` and in the `model` field of the `requests.http`
> payloads. If you rename the agent, change all three.

> **Note on isolation keys.** Foundry's hosting layer scopes sessions by two
> headers: `x-agent-user-isolation-key` and `x-agent-chat-isolation-key`. In
> production Foundry injects them per user/conversation. Locally,
> `AgentHost.CreateBuilder` supplies a default session-isolation provider, so
> **you don't need to send these headers** for a plain `dotnet run` — requests
> succeed without them. `requests.http` still sets them via `@userKey` /
> `@chatKey` (handy for simulating separate conversations — change `@chatKey`
> to start a fresh one), but they're optional locally.

---

## Step 3 — Run it as a container (optional)

> ℹ️ **Optional.** The deploy path in Step 4 builds the image remotely with
> `az acr build`, so you don't need a working local container to ship. Use this
> step only if you want to validate the image build itself.

```bash
# Build the image
docker build -t trip-planner:local .
```

> **Heads-up on local container auth.** `Program.cs` now uses
> `DefaultAzureCredential` only (matching the canonical hello-world sample — the
> old `AZURE_BEARER_TOKEN` / `StaticTokenCredential` shim was removed). A bare
> `docker run` has no `az login`, no managed-identity endpoint, and no token
> cache, so the agent can't authenticate to Foundry from inside the container.
> In production Foundry assigns the hosted agent its own **managed identity**,
> which `DefaultAzureCredential` picks up automatically. For local validation,
> prefer `dotnet run` (Step 2), which runs under your `az login` identity.

---

## Step 4 — Deploy to Foundry (Bicep + REST API)

Once the container runs cleanly locally, push it into Foundry. Foundry takes
care of the runtime (scaling, ingress, auth, session isolation, observability)
— you just hand it an image. The provisioning around that still needs a Foundry
project, a container registry to publish to, and the right RBAC.

Most enterprises ship that with **infrastructure-as-code + a REST call from a
pipeline**, not an interactive CLI — so that's the path this module teaches. The
[`cicd/`](cicd) folder packages it as a single script,
[`deploy.sh`](cicd/deploy.sh).

> 🧭 **How to read this step.** Lines marked **▶️ Do** are commands you run or
> actions you take. Lines marked **✅ Verify** are checks to confirm the step
> worked before moving on. Everything else is background context.

**Background — what `deploy.sh` does for you** (you don't run these individually;
the script orchestrates them):

1. provisions a complete **standalone** stack with **Bicep** — Foundry account +
   project, the `gpt-4o-mini` deployment, an ACR with a ManagedIdentity
   connection, and Log Analytics + Application Insights for later observability
   modules;
2. grants the RBAC for you — **AcrPull** + **Foundry User** to the project's
   managed identity (so the container can pull the image and call the model), and
   **Foundry Project Manager** to you (so the version-create call succeeds);
3. builds and pushes the image with **`az acr build`** (no local Docker daemon);
4. registers the agent version through the Foundry **REST API**; and
5. polls until the version reports `active`.

> **Standalone by design.** By default Module 12 provisions its *own* Foundry
> account rather than reusing `tripbot-project`. A fresh account auto-provisions
> the hosted-agent runtime, which avoids the capability-host provisioning dance
> the `azd` path needs.
>
> **Want it next to your prompt agent?** You can instead deploy the hosted agent
> into the *existing* `tripbot-project` (the one Modules 1–11 use) so both agents
> appear together — see
> [Deploy into an existing Foundry](cicd/README.md#deploy-into-an-existing-foundry-side-by-side-with-modules-111).
> It reuses the base account (redeployed on the `2026-03-01` API) and adds only
> an ACR + the project's image-pull/model RBAC.

#### 4a. Sign in and select your subscription

**▶️ Do** — sign in and pick the subscription the stack should land in:

```bash
az login
az account set --subscription <your-subscription-id>
```

**✅ Verify** — the correct subscription is active:

```bash
az account show --query "{name:name, id:id}" -o table
```

#### 4b. Run the deploy script

**▶️ Do** — deploy the stack. A fresh `ENVIRONMENT_NAME` keeps these resources in
their own resource group (`rg-<ENVIRONMENT_NAME>`), separate from Modules 1–11:

```bash
cd src/12-foundry-hosted
ENVIRONMENT_NAME=tripbot-cicd LOCATION=eastus2 ./cicd/deploy.sh
```

> **Heads-up — permissions & timing.** Your account needs **Owner** (or
> **Contributor + Role Based Access Control Administrator**) at the subscription
> scope, because the script creates a resource group *and* role assignments. The
> first run takes ~5–10 minutes.

**✅ Verify** — when the script finishes it prints the **project endpoint** and
the **REST invoke URL**, and the agent version reports `active`. Copy both
printed values — you'll use them in [Calling the deployed agent](#calling-the-deployed-agent).

#### 4c. Redeploy after a code change (optional)

**▶️ Do** — re-register a new version without re-running the Bicep:

```bash
ENVIRONMENT_NAME=tripbot-cicd LOCATION=eastus2 ./cicd/deploy.sh --skip-infra
```

**✅ Verify** — the script reports a **new version** for `trip-planner` and polls
it to `active`. The previous version stays addressable until the new one rolls out.

> ℹ️ **Same script in CI.** It runs unchanged in **GitHub Actions** via
> [`.github/workflows/deploy-hosted-agent.yml`](../../.github/workflows/deploy-hosted-agent.yml)
> (OIDC login, `workflow_dispatch`). See [`cicd/README.md`](cicd/README.md) for
> the resource breakdown, the CI/OIDC setup, the deploy flags and model/region
> parameters, and image-pull troubleshooting.

### Identity & RBAC — handled for you

A hosted agent authenticates to Azure as a **managed identity** when its
container pulls its image, calls the model, reads session history, or hits any
other resource. In this standalone stack the agent runs as the **Foundry
project's system-assigned identity**, and the Bicep wires up everything it needs:

| Role | Assignee | Scope | Why |
| --- | --- | --- | --- |
| **AcrPull** | project identity | the ACR | pull the agent image |
| **Foundry User** | project identity | the Foundry account | call the model endpoint |
| **Foundry Project Manager** | you (the deployer) | the project | create agent versions |

That's the whole RBAC story — there's nothing to fix up afterwards. This is the
big advantage of the IaC path over a hand-rolled `az rest` POST, which would
otherwise leave you chasing a `401` (deployer can't create the version) or a
`500` `ManagedIdentityCredential` failure (agent identity can't call the model)
on your own.

> ℹ️ **This table is the *deployed* (cloud) agent.** Running the agent **locally**
> with `dotnet run` is different — there it calls the model as *your* user, so
> you need **Foundry User at the account level** yourself. See the
> [Local RBAC callout in Step 2](#step-2--run-it-locally-with-dotnet-run).

> **Role rename.** Foundry User / Foundry Project Manager were formerly *Azure
> AI User* / *Azure AI Project Manager* — you may still see the old names in the
> portal. The role IDs and permissions are unchanged.
>
> **Wiring up downstream resources** (Key Vault, Storage, AI Search, …)? Grant
> them to the **same project identity** by extending
> [`cicd/infra/modules/foundry-project.bicep`](cicd/infra/modules/foundry-project.bicep)
> with the role assignment, so it stays reproducible.

### Calling the deployed agent

The deployed endpoint is **not** `https://{agent-host}/responses` like the local
server. It's scoped under your project endpoint with an agent subpath, and the
refreshed preview requires a feature header.

**▶️ Do** — invoke the agent (use the `PROJECT_ENDPOINT` printed by `deploy.sh`):

```bash
# Both values are printed at the end of deploy.sh
PROJECT_ENDPOINT="https://<account>.services.ai.azure.com/api/projects/trip-project"
TOKEN=$(az account get-access-token --resource https://ai.azure.com/ --query accessToken -o tsv)

curl -sS -X POST \
  "$PROJECT_ENDPOINT/agents/trip-planner/endpoint/protocols/openai/responses?api-version=2025-11-15-preview" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Foundry-Features: HostedAgents=V1Preview" \
  -H "Content-Type: application/json" \
  -d '{"input":"Plan a 3-day trip to Tokyo.","model":"trip-planner","store":true}'
```

**✅ Verify** — you get back a JSON Responses envelope containing a Tokyo itinerary.

> ⚠️ **The `Foundry-Features: HostedAgents=V1Preview` header is mandatory during
> preview** — without it the endpoint returns HTTP 400 `preview_feature_required`.

**▶️ Do (alternative) — test in the portal** instead of curl: open
[ai.azure.com](https://ai.azure.com), go to your project → **Agents**, click
`trip-planner` to chat and to see its endpoint URL, container image, and live logs.

### Gotcha on your first invoke — provisioning lag

The *first* hosted agent you deploy triggers Foundry to provision the managed
runtime behind the scenes. The agent version flips to `active` several minutes
*before* its endpoint is actually reachable, so an early invoke can return
`HTTP 404 "Subdomain does not map to a resource"`. This is normal first-deploy
timing — wait a few minutes and retry. Subsequent deploys don't have this lag.

### Alternative — deploy with `azd` (local dev convenience)

Prefer an interactive CLI for quick experiments? `azd` can scaffold and deploy
the same agent. It's handy for a one-off local loop, but it's **not** the
recommended path for repeatable/enterprise delivery — it pulls in the
`azure.ai.agents` extension, needs a one-time capability-host provisioning step,
surfaces RBAC failures as warnings, and caches conversations locally.

```bash
cd src/12-foundry-hosted
azd auth login
azd ext install azure.ai.agents        # needs azd ≥ 1.23.0

# Reuse an existing Foundry project (e.g. Module 11's) by ARM resource ID
PROJECT_ID=$(az cognitiveservices account show \
  -g <your-rg> -n tripbot-foundry --query id -o tsv)/projects/tripbot-project

azd ai agent init -m ./agent.manifest.yaml --src . --force \
  --agent-name trip-planner -p "$PROJECT_ID" -d gpt-4o-mini
azd up
```

> **One-time capability host.** `azd up` doesn't provision the **Agents
> capability host** hosted agents require, so create it once at **both** account
> and project scope first (`capabilityHostKind: Agents`,
> `enablePublicHostingEnvironment: true`, api-version `2025-10-01-preview`), poll
> until `provisioningState: Succeeded`, then run `azd up`. The Bicep path skips
> this entirely because a fresh account auto-provisions the runtime.

Two `azd`-specific snags to know:

- **RBAC failures are silent.** The `azd ai agent` preview reports `azd up`
  success even if the agent identity ends up with no **Foundry User** role on the
  account (symptom: playground HTTP 500 `ManagedIdentityCredential`). Fix:
  `az role assignment create --assignee-object-id <agent-oid> --role "Foundry User" --scope <account-id>`,
  then wait 5–15 min for AAD propagation.
- **Stale conversation cache.** `azd ai agent invoke` caches the conversation per
  agent version in `~/.azd/config.json`; if your first call failed before the
  conversation was created you'll get `HTTP 404 "Conversation '<id>' not found"`
  on every retry. Clear it with
  `azd config unset extensions.ai-agents.conversations` and
  `azd config unset extensions.ai-agents.sessions`, then invoke again.

The generated `azure.yaml`, `infra/`, and `.azure/` folders are git-ignored —
they're per-developer build state, not part of the learning material.

---

## Step 5 — Your turn 🛠️

### 🟢 Starter — Tune the persona

Edit the `instructions:` string in `Program.cs` so TripBot specializes in
**budget travel under $1000/trip**. Run locally, hit it from `requests.http`,
and confirm the responses change. Then redeploy with
`./cicd/deploy.sh --skip-infra` — Foundry creates a new **version** of the same
agent, so the old version stays addressable while the new one rolls out.

### 🟡 Intermediate — Add a function tool

The agent in `Program.cs` is configured inline via `AsAIAgent(...)`. Switch to
the builder form so you can attach a tool:

```csharp
AIAgent agent = client.GetChatClient(deployment)
    .CreateAIAgent(
        instructions: "...",
        name: agentName,
        tools: [AIFunctionFactory.Create(GetExchangeRate, "get_exchange_rate")]);
```

Define `GetExchangeRate(string from, string to)` (returning a hardcoded rate
is fine for the lesson). Ask the agent for an itinerary "with costs in EUR"
and confirm it calls the tool. This proves the **same** tool plumbing from
Modules 02 and 07 still works inside a Foundry-hosted container.

### Stretch — Lift the weather agent into a second container

Module 10's `weather` agent is still self-hosted. Create
`src/12-foundry-hosted-weather/` (copy this folder), change the agent name,
instructions, description, and `agent.manifest.yaml` name fields to `weather`,
and deploy it as a **second** hosted agent. Then call both deployed agents from
a single client to confirm two hosted agents can co-exist under one Foundry
project — the same separation-of-concerns Module 10 had, but now operated by
Foundry.

> **Hint:** Foundry hosted agents are 1:1 with containers. If you want them
> to call each other, give one the other's endpoint as configuration and have
> it call it like any other HTTP service — or register a Foundry Prompt Agent
> (Module 11) that uses both as tools.

---

## Step 6 — Build it from scratch (optional)

Want to prove you understand it?

```bash
# In src/12-foundry-hosted/
cp Program.scaffold.cs Program.cs   # overwrites the solution
dotnet run                          # fails — fill in the TODOs
```

---

## Key concepts

### Self-hosted vs Foundry-hosted

| | Self-hosted (Module 10) | Foundry-hosted (Module 12) |
|---|---|---|
| Who runs the process | You (App Service, ACA, AKS, VM…) | Microsoft / Foundry |
| Transport | A2A | OpenAI Responses |
| Agents per process | Many | One per container |
| Identity | Whatever you wire up | Managed identity assigned by Foundry |
| Discovery | A2A `AgentCard` | Foundry portal + project listing |
| Best for | Mixed A2A ecosystems, full control | Foundry-native ecosystems, low ops |

### What `AddFoundryResponses(agent)` adds

- Registers the agent + the Responses request/response shape in DI
- Wires up the session/conversation handling that Foundry expects when it routes
  follow-up calls with `previous_response_id`
- Installs the User-Agent and identity policies the Foundry runtime relies on

### What `agent.manifest.yaml` controls

- `template.kind: hosted` — tells Foundry this is a container-backed agent (vs
  a Prompt Agent from Module 11)
- `template.protocols` — Foundry hosted agents support `responses`,
  `invocations`, and `activity` (Bot Framework). A2A is **not** in this list,
  which is why Module 10's endpoint can't be lifted directly.
- `template.resources.cpu` / `template.resources.memory` — container sizing
- `template.environment_variables` — what platform values get exposed into the
  container. Here we template only the model deployment name
  (`AZURE_OPENAI_DEPLOYMENT_NAME`); Foundry **auto-injects**
  `FOUNDRY_PROJECT_ENDPOINT` (and `APPLICATIONINSIGHTS_CONNECTION_STRING`), so
  those are *not* declared here. `Program.cs` reads `FOUNDRY_PROJECT_ENDPOINT`
  and normalizes it to the account host.
- Top-level `resources:` — declares downstream Azure resources (here: the
  model deployment). The deploy path uses this to wire up / provision them.

---

## Anti-patterns to avoid

❌ **Trying to expose A2A from a Foundry-hosted container.** The platform only
routes the protocols listed in `agent.manifest.yaml`, and A2A isn't a valid value.

❌ **Putting multiple agents in one hosted container.** Foundry's agent-name
routing is per-container — only the agent matching `AGENT_NAME` will be
reachable. Use one container per agent (or use Module 11's Prompt Agent to
fan out to tools).

❌ **Hardcoding the agent name.** Always read `AGENT_NAME` from the environment
so the platform can stage versions or rename without code changes.

❌ **Deploying every code change as a new agent.** Re-run
`./cicd/deploy.sh --skip-infra` against the same manifest to push new
**versions** of the same agent — clients keep their URLs and Foundry handles the
rollover.

## References

- [Migrate Hosted agents from initial public preview](https://learn.microsoft.com/azure/foundry/agents/how-to/migrate-hosted-agent-preview) — the doc that defines the refreshed-preview pattern this module follows
- [Microsoft-Foundry .NET Hosted Agent samples](https://github.com/microsoft-foundry/foundry-samples/tree/main/samples/csharp/hosted-agents/agent-framework)
- [`Microsoft.Agents.AI.Foundry.Hosting` on NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI.Foundry.Hosting)
- [`Azure.AI.AgentServer.Core` on NuGet](https://www.nuget.org/packages/Azure.AI.AgentServer.Core)
- [Foundry Hosted Agents overview](https://learn.microsoft.com/azure/foundry/agents/overview)
- [Agent manifest schema](https://raw.githubusercontent.com/microsoft/AgentSchema/refs/heads/main/schemas/v1.0/AgentManifest.yaml)
- [`azd ai agent` reference](https://learn.microsoft.com/azure/developer/azure-developer-cli/reference)

---

**→ Back to: [Repo Root](../../README.md)**
