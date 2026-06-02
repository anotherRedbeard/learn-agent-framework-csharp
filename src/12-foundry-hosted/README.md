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
- What `agent.yaml` declares (`kind`, `protocols`, `resources`) and how Foundry
  uses it to provision the platform agent version
- The full local-loop: `dotnet run` → `docker run` → `azd ai agent deploy`

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

- **.NET 10 SDK** (this is the only module that requires .NET 10 — the Foundry
  Hosting preview targets `net10.0`)
- **Docker** (or Podman) for the local container loop
- An **Azure AI Foundry project** with a deployed chat model (`gpt-4o-mini`
  works well) — same project you used for Module 11 is fine
- **Azure CLI** logged in: `az login`
- **Azure Developer CLI** with the AI agent extension installed: `azd version`
  ≥ 1.18 (see [azd installation](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd))
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
builder.Services.AddFoundryResponses(agent);
app.MapFoundryResponses();
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

Set config (same env vars the rest of the repo uses — if you already exported
them per [`docs/prerequisites.md`](../../docs/prerequisites.md), skip this):

```bash
cd src/12-foundry-hosted
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" \
  "https://<account>.services.ai.azure.com"
dotnet user-secrets set "AZURE_OPENAI_DEPLOYMENT_NAME" "gpt-4o-mini"
dotnet run
```

The agent starts on `http://localhost:8088`. Open `requests.http` and send the
first request. You should get back a JSON envelope shaped like the OpenAI
Responses API.

> **Note on `AGENT_NAME`.** When Foundry runs the container in the cloud it
> injects `AGENT_NAME` so the same image can back multiple versions. Locally
> we default to `trip-planner` — the same value that lives in `agent.yaml` and
> in the `model` field of the `requests.http` payloads. If you rename the
> agent, change all three.

> **Note on isolation keys.** Foundry also injects `x-agent-user-isolation-key`
> and `x-agent-chat-isolation-key` headers on every request so the hosting
> layer can scope sessions per user/conversation. Without those headers the
> default provider returns null and every request 500s. `Program.cs` registers
> a small `LocalDevIsolationKeyProvider` that supplies stable fallback keys
> (`local-dev-user` / `local-dev-chat`) when headers are missing and passes
> real Foundry-supplied values through untouched when they're present — so
> the same code works locally and in production.

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
> way to do `az login`. Foundry solves this in production with a **managed
> identity** assigned to the hosted agent. Locally we substitute a bearer token
> from your already-authenticated host.

---

## Step 4 — Deploy to Foundry

Once the container runs cleanly locally, push it into Foundry:

```bash
cd src/12-foundry-hosted

# Point azd at your Foundry project (one-time per shell)
azd env set AZURE_OPENAI_ENDPOINT \
  "https://<account>.services.ai.azure.com"

# Build, push to a registry, register with Foundry
azd ai agent deploy
```

`azd ai agent deploy` reads `agent.yaml`, builds and pushes the image, creates
(or updates) the platform agent version, assigns a managed identity, and waits
until the version status is `Active`. Provisioning a new version typically
takes 2–5 minutes.

Verify in [ai.azure.com](https://ai.azure.com) — open your project, go to
**Agents**, and you should see `trip-planner` listed alongside Module 11's
Prompt Agent. Click it to see its endpoint URL, container image, and live logs.

To call the deployed agent from your own code, swap the endpoint in
`requests.http` (or any client) to the Foundry-issued URL — the Responses
payload shape is identical.

---

## Step 5 — Your turn 🛠️

### 🟢 Starter — Tune the persona

Edit the `instructions:` string in `Program.cs` so TripBot specializes in
**budget travel under $1000/trip**. Run locally, hit it from `requests.http`,
and confirm the responses change. Then redeploy with `azd ai agent deploy` —
Foundry creates a new **version** of the same agent, so the old version stays
addressable while the new one rolls out.

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

### 🔴 Stretch — Lift the weather agent into a second container

Module 10's `weather` agent is still self-hosted. Create
`src/12-foundry-hosted-weather/` (copy this folder), change the agent name,
instructions, description, and `agent.yaml.name` to `weather`, and deploy it
as a **second** hosted agent. Then call both deployed agents from a single
client to confirm two hosted agents can co-exist under one Foundry project —
the same separation-of-concerns Module 10 had, but now operated by Foundry.

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

### What `agent.yaml` controls

- `kind: hosted` — tells Foundry this is a container-backed agent (vs a Prompt
  Agent from Module 11)
- `protocols` — Foundry hosted agents support `responses` and `activityprotocol`
  (Bot Framework). A2A is **not** in this list, which is why Module 10's
  endpoint can't be lifted directly.
- `resources.cpu` / `resources.memory` — provisioning sizing for the container

---

## Anti-patterns to avoid

❌ **Trying to expose A2A from a Foundry-hosted container.** The platform only
routes the protocols listed in `agent.yaml`, and A2A isn't a valid value.

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

❌ **Deploying every code change as a new agent.** Use `azd ai agent deploy`
on the same `agent.yaml` to push new **versions** of the same agent — clients
keep their URLs and Foundry handles the rollover.

## References

- [Microsoft Agent Framework — Foundry Hosting samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/04-hosting/FoundryHostedAgents/responses)
- [`Microsoft.Agents.AI.Foundry.Hosting` on NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI.Foundry.Hosting)
- [Foundry Hosted Agents overview](https://learn.microsoft.com/azure/foundry/agents/overview)
- [Container Agent schema](https://raw.githubusercontent.com/microsoft/AgentSchema/refs/heads/main/schemas/v1.0/ContainerAgent.yaml)
- [`azd ai agent` reference](https://learn.microsoft.com/azure/developer/azure-developer-cli/reference)

---

**→ Back to: [Repo Root](../../README.md)**
