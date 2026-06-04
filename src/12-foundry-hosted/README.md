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
- The full local-loop: `dotnet run` → `docker run` → `azd up`

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

- **Docker** (or Podman) for the local container loop
- An **Azure AI Foundry project** with a deployed chat model (`gpt-4o-mini`
  works well) — same project you used for Module 11 is fine
- **Azure CLI** logged in: `az login`
- **Azure Developer CLI** ≥ **1.23.0** with the Foundry agents extension installed:
  ```bash
  azd version           # confirm ≥ 1.23.0
  azd ext install azure.ai.agents
  ```
  (Required by the refreshed-preview hosting backend — earlier `azd` versions
  call the retired hosting APIs.)
- A container registry Foundry can pull from. `azd ai agent deploy` will create
  one for you on first deploy if you don't have one.

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

> ⚠️ **Don't run the streaming request from the VS Code REST Client.** The
> agent's `/responses` endpoint with `"stream": true` returns Server-Sent
> Events token-by-token, but the VS Code REST Client extension does **not**
> render SSE incrementally — it buffers the entire stream in memory and only
> displays anything after the server closes the connection. For a long
> completion that means 20–40 seconds of "Waiting…" with no output, which
> looks identical to a hung request. The server is fine; the client is the
> bottleneck. To actually watch tokens arrive, use the `curl --no-buffer`
> command provided in `requests.http` from a terminal instead. Stick to the
> non-streaming requests when using the REST Client UI.

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
> required headers: `x-agent-user-isolation-key` and `x-agent-chat-isolation-key`.
> In production Foundry injects them per user/conversation; locally you pick
> any stable strings. **Without these headers every request 500s** with
> `HostedSessionIsolationKeyProvider returned null` in the server console.
> `requests.http` already sets them via `@userKey` / `@chatKey` variables —
> change `@chatKey` to start a fresh conversation. If you `curl` directly,
> add `-H "x-agent-user-isolation-key: local-dev-user" -H "x-agent-chat-isolation-key: local-dev-chat"`.

---

## Step 3 — Run it as a container

Same agent, packaged the way Foundry will run it:

```bash
# 1. Build the image
docker build -t trip-planner:local .

# 2. Generate a short-lived Azure token to inject (managed identity is what
#    Foundry uses in the cloud — locally we fake it with your az login token)
export AZURE_BEARER_TOKEN=$(az account get-access-token \
  --resource https://ai.azure.com --query accessToken -o tsv)

# 3. Run
cp .env.example .env   # then edit .env to fill in your endpoint
docker run --rm -p 8088:8088 \
  --env-file .env \
  -e AZURE_BEARER_TOKEN=$AZURE_BEARER_TOKEN \
  trip-planner:local
```

Re-run the requests in `requests.http`. Same agent, now coming from a container.

> **Why a bearer token?** `DefaultAzureCredential` inside the container has no
> way to do `az login` — there's no Azure CLI, no managed identity endpoint, no
> Visual Studio token cache. Foundry solves this in production by assigning the
> hosted agent its own **managed identity**, which `DefaultAzureCredential`
> picks up automatically. Locally we substitute a bearer token minted from your
> already-authenticated host: `Program.cs` notices `AZURE_BEARER_TOKEN` is set
> and uses a small `StaticTokenCredential` that hands it back verbatim, instead
> of going through `DefaultAzureCredential`. Tokens from `az account
> get-access-token` last ~60 minutes — re-export and re-run `docker run` if
> your session lasts longer.

---

## Step 4 — Deploy to Foundry

Once the container runs cleanly locally, push it into Foundry. Foundry takes
care of the runtime (scaling, ingress, auth, session isolation, observability)
— you just hand it an image. The provisioning around that, though, still
needs a Foundry project + container registry to publish to. `azd ai agent init`
handles both.

There are two flavors depending on whether you're starting fresh or reusing
existing infra:

### Flavor A — Reuse the Foundry project you already have (recommended for this repo)

You set up `tripbot-project` back in Module 11. Pointing `-p` at it skips
the greenfield Bicep (no new ACR, App Insights, AI Search — Foundry reuses
what the project already has) and just wires `trip-planner` in as a new
agent.

```bash
cd src/12-foundry-hosted
azd auth login

# Grab the full ARM resource ID of your existing Foundry project
PROJECT_ID=$(az cognitiveservices account show \
  -g <your-rg> -n tripbot-foundry --query id -o tsv)/projects/tripbot-project

# In-place init: scaffold azure.yaml + infra/ next to this module's source.
# --src . tells azd "the agent code is already here, don't download it".
# --force is needed because the manifest sits inside the src tree.
azd ai agent init \
  -m ./agent.manifest.yaml \
  --src . \
  --force \
  --agent-name trip-planner \
  -p "$PROJECT_ID" \
  -d gpt-4o-mini

# Build image, push to the project's registry, register the agent version
azd up
```

The generated `azure.yaml`, `infra/`, and `.azure/` folders are git-ignored
at the repo root — they're per-developer build state, not part of the
learning material.

### Flavor B — Greenfield (let azd provision everything)

If you don't have a Foundry project yet, drop the `-p`/`-d` flags. `azd up`
will then provision a brand-new project, container registry, App Insights,
and model deployment from the Bicep that `init` scaffolded. Useful for a
clean demo environment; overkill if you already have Module 11's project.

```bash
cd src/12-foundry-hosted
azd auth login
azd ai agent init -m ./agent.manifest.yaml --src . --force --agent-name trip-planner
azd up
```

---

`azd up` builds the image, pushes it to the project's registry, and creates
(or updates) the hosted agent version with its own dedicated managed
identity. Provisioning a new version typically takes 2–5 minutes; wait
until the version status reads `active`.

Verify in [ai.azure.com](https://ai.azure.com) — open your project, go to
**Agents**, and you should see `trip-planner` listed alongside Module 11's
Prompt Agent. Click it to see its endpoint URL, container image, and live logs.

### Calling the deployed agent

The deployed endpoint is **not** `https://{agent-host}/responses` like the
local server. It's scoped under your project endpoint with an agent subpath,
and the refreshed preview requires a feature header:

```http
POST {project_endpoint}/agents/trip-planner/endpoint/protocols/openai/responses?api-version=2025-05-01
Authorization: Bearer <token>
Foundry-Features: HostedAgents=V1Preview        # ← required during preview
Content-Type: application/json

{ "input": "Plan a 3-day trip to Tokyo.", "model": "trip-planner" }
```

Mint a token with:

```bash
az account get-access-token --resource https://ai.azure.com --query accessToken -o tsv
```

The `Foundry-Features` header is mandatory — without it the endpoint returns
HTTP 400 `preview_feature_required`. The SDK sets it automatically; raw REST
clients (curl, VS Code REST Client) must add it themselves. See the "Deployed
agent" example in `requests.http`.

### Identity & RBAC for the deployed agent

The refreshed preview gives each hosted agent its own **Agent Entra Identity**
at deploy time (visible in the Foundry portal at the agent level — _not_ the
same as the project's system-assigned MI). When the container calls the model
or any other Azure resource, it authenticates as that identity via
`DefaultAzureCredential`.

To actually let the agent call the model you must:

1. **Grant RBAC to the agent's identity** on the Foundry account
   (`tripbot-foundry`). The required role is **Foundry User** (recently renamed
   from "Azure AI User" — same role ID, both names work during the rollout).
   In the portal: Foundry resource → Access control (IAM) → Add role
   assignment → Foundry User → assign to the agent's principal (find its
   object ID on the agent's identity page).
2. **Wait 5–15 minutes** for AAD role propagation. The first few requests
   after grant can return `ManagedIdentityCredential` failures even with the
   role assigned — this is normal AAD behavior, not a misconfiguration.
3. If your agent reads from Key Vault, Storage, etc., grant those resources
   RBAC to the **same** agent identity (not the project MI).

`azd up` provisions the agent identity but does **not** assign downstream
RBAC for you — that step is yours.

---

## Step 5 — Your turn 🛠️

### 🟢 Starter — Tune the persona

Edit the `instructions:` string in `Program.cs` so TripBot specializes in
**budget travel under $1000/trip**. Run locally, hit it from `requests.http`,
and confirm the responses change. Then redeploy with `azd up` — Foundry creates
a new **version** of the same agent, so the old version stays addressable while
the new one rolls out.

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
  container (we map `FOUNDRY_PROJECT_ENDPOINT` → `AZURE_OPENAI_ENDPOINT` so the
  code matches the rest of the repo)
- Top-level `resources:` — declares downstream Azure resources (here: the
  model deployment). `azd ai agent init` uses this to wire up / provision them.

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

❌ **Skipping the local container loop.** `dotnet run` validates your agent
logic; only `docker run` validates that your image actually starts under a
managed-identity-style credential. Skipping it pushes failures into Foundry,
where they cost minutes per round-trip.

❌ **Deploying every code change as a new agent.** Re-run `azd up` against the
same manifest to push new **versions** of the same agent — clients keep their
URLs and Foundry handles the rollover.

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
