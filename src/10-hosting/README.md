# Module 10 — Hosting

**Concept:** Host agents behind ASP.NET Core endpoints so other apps, agents, and tools can discover and call them over A2A.

## What you'll learn

- How `AddAIAgent()` registers hosted agents with dependency injection
- How `MapA2AHttpJson()` (the HTTP-JSON `MapA2A` helper) exposes an agent as an A2A endpoint
- How an `AgentCard` describes a hosted agent for discovery
- Why `contextId` matters for conversation continuity over HTTP
- How to expose a workflow by converting it to an agent with `AddAsAIAgent()`

## When to use this pattern

Hosted agents are the right pattern when:
- You need another service, UI, or agent to call your agent over HTTP
- You want agents to run as independently deployable ASP.NET Core services
- You need A2A-compatible discovery and message endpoints
- You want DI-managed agents that can share clients, configuration, and lifecycle

---

## Step 1 — Run it first

This module reads `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_DEPLOYMENT_NAME` from `builder.Configuration`, which checks **both** environment variables and `dotnet user-secrets`. If you already exported them in your shell (see [`docs/prerequisites.md`](../../docs/prerequisites.md)), they're already in scope and you can skip straight to `dotnet run`.

If you'd rather scope credentials to this project only, set them via user-secrets:

```bash
cd src/10-hosting
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://your-project.services.ai.azure.com"
dotnet user-secrets set "AZURE_OPENAI_DEPLOYMENT_NAME" "gpt-4o-mini"
```

> User-secrets are **per-project** — each module has its own keyring — while exported environment variables apply across every module in the same shell session.

Then start the host:

```bash
cd src/10-hosting
dotnet run
```

Open `requests.http` and send the AgentCard and message requests. You should see hosted endpoints for the weather agent, travel agent, and trip-planning workflow.

---

## Step 2 — Code walkthrough

Open `Program.cs` and read through it alongside these explanations.

### Register the shared chat client

```csharp
IChatClient chatClient = new AIProjectClient(
        new Uri(endpoint),
        new DefaultAzureCredential())
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(deploymentName);

builder.Services.AddKeyedSingleton("chat-model", chatClient);
```

- `AIProjectClient` connects to Azure AI with `DefaultAzureCredential`
- `.AsIChatClient()` adapts the model deployment into the chat abstraction used by hosted agents
- `AddKeyedSingleton()` stores the client under a key so multiple agents can share it
- The key must match the `chatClientServiceKey` used when registering agents

### Register hosted agents

```csharp
var weatherAgent = builder.AddAIAgent(
    "weather",
    instructions: "You are a destination weather expert...",
    description: "Provides weather conditions for travel destinations.",
    chatClientServiceKey: "chat-model")
    .AddA2AServer();
```

- `AddAIAgent()` registers the agent in dependency injection instead of creating it inline
- The first argument becomes the agent key used by DI and hosting
- `instructions` define the agent's behavior for every request
- `description` becomes part of the hosted agent metadata
- `.AddA2AServer()` registers an `A2AServer` in DI for this agent — required before `MapA2AHttpJson()` can wire up the HTTP endpoint

> **Why not create the agent directly?** Hosted agents should participate in the ASP.NET Core DI lifecycle. `AddAIAgent()` lets agents share configured services, keyed clients, and deployment-time configuration.

> **Why is `.AddA2AServer()` separate from `MapA2AHttpJson()`?** Registration (DI) and routing (HTTP) are deliberately split. `.AddA2AServer()` makes the agent *available* over A2A; `MapA2AHttpJson()` *exposes* it at a URL. This lets the same A2A server back multiple transports (HTTP-JSON, JSON-RPC) or stay headless if you don't need HTTP at all.

### Register a workflow as an agent

```csharp
var planningWorkflow = builder.AddWorkflow("trip-planning", (sp, key) =>
{
    var weather = sp.GetRequiredKeyedService<AIAgent>("weather");
    var travel = sp.GetRequiredKeyedService<AIAgent>("travel");
    return AgentWorkflowBuilder.BuildSequential(key, [weather, travel]);
});

var planningWorkflowAsAgent = planningWorkflow.AddAsAIAgent()
    .AddA2AServer();
```

- `AddWorkflow()` registers a workflow that composes multiple agents
- `GetRequiredKeyedService<AIAgent>()` resolves the hosted agents from DI
- `BuildSequential()` sends the request through weather first, then travel
- `AddAsAIAgent()` wraps the workflow so it can be exposed through A2A like any other agent
- `.AddA2AServer()` registers the workflow-as-agent for A2A just like a regular agent

### Expose A2A endpoints

```csharp
MapAgentWithCard(app, "weather", "/a2a/weather",
    description: "Provides weather conditions for travel destinations.");
MapAgentWithCard(app, "travel", "/a2a/travel",
    description: "Creates travel itineraries and trip recommendations.");
MapAgentWithCard(app, "trip-planning", "/a2a/trip-planning",
    description: "Sequential workflow: gathers weather, then builds a trip itinerary.");

// ...

static void MapAgentWithCard(WebApplication app, string agentName, string path, string description)
{
    var server = app.Services.GetRequiredKeyedService<A2AServer>(agentName);
    var card = new AgentCard { Name = agentName, Description = description, Version = "1.0.0" };
    app.MapHttpA2A(server, card, path);
}
```

- We *would* call `app.MapA2AHttpJson(agent, path)` here, but the current preview of that helper hardcodes `AgentCard { Name = "A2A Agent" }` and offers no card hook
- `MapAgentWithCard` drops down to `A2A.AspNetCore`'s `MapHttpA2A(server, card, path)`, supplying a real `AgentCard` so clients see meaningful name/description/version
- `GET /a2a/weather/card` returns that `AgentCard`
- `POST /a2a/weather/message:stream` sends a message to the weather agent
- A2A clients use the `AgentCard` to discover the agent before sending messages

> **Why not just use `MapA2AHttpJson()`?** It's the right API surface but the
> preview library doesn't expose a card-customization callback yet. Once it does,
> swap `MapAgentWithCard` for `app.MapA2AHttpJson(agent, path, configureCard: ...)`.

### Keep context over HTTP

```json
"contextId": "weather-conversation-1"
```

- `contextId` is the conversation key in A2A requests
- Reuse the same `contextId` for follow-up questions
- Change the `contextId` when you want a fresh conversation

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Change an AgentCard's metadata

Edit one of the `MapAgentWithCard(...)` calls in `Program.cs` (or change `Version` inside the helper). Run the app, request that agent's card from `requests.http`, and verify the metadata changed.

### 🟡 Intermediate — Add a new hosted agent

Register a third agent with `AddAIAgent()`, chain `.AddA2AServer()`, and expose it with `MapAgentWithCard()` at its own endpoint, such as `/a2a/packing`. Add matching requests to `requests.http` and verify its `AgentCard`, `contextId`, and message endpoint work.

### 🔴 Stretch — Add an API key check

Add an auth handler or lightweight API key check around the A2A endpoints. Require a header such as `X-API-Key`, reject missing or incorrect keys, and keep the key in user secrets or environment variables — never in source.

> **Hint:** Hosting turns your agent into an API. Once an endpoint exists, think about discovery, authentication, rate limits, and which clients should be allowed to call it.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Delete `Program.cs` contents and rebuild from `Program.scaffold.cs`:

```bash
# In src/10-hosting/
cp Program.scaffold.cs Program.cs   # overwrites the solution with the scaffold
dotnet run                           # will fail — that's expected, fill in the TODOs
```

---

## Key concepts

### What hosting adds
An ASP.NET Core host gives your agent HTTP endpoints, dependency injection, configuration, middleware, and deployment boundaries.

### What `AddAIAgent()` adds
- **DI lifecycle** — agents are registered and resolved by the host
- **Shared services** — agents can reuse keyed chat clients and configuration
- **Hosting metadata** — descriptions and registrations can become discoverable through A2A

### What A2A adds
- **AgentCard discovery** — clients can inspect what an agent is before calling it
- **Standard message routes** — clients use predictable HTTP endpoints
- **Conversation continuity** — `contextId` keeps related requests together

## Anti-patterns to avoid

❌ **Creating hosted agents inline** — use `AddAIAgent()` so agents participate in DI and hosting.

❌ **Hardcoding credentials or API keys** — use user secrets in development and managed identity, Key Vault, or environment variables in production.

❌ **Exposing A2A endpoints without access control** — discovery and message endpoints are still APIs and should be protected in real deployments.

❌ **Changing `contextId` on every request** — the agent will treat each call as a brand-new conversation.

❌ **Forgetting `.AddA2AServer()`** — `MapA2AHttpJson()` will throw at startup ("No A2AServer is registered for agent 'X'"). Registration in DI and routing on the endpoint are two separate steps.

❌ **Exposing a workflow directly** — wrap workflows with `AddAsAIAgent()` (and chain `.AddA2AServer()`) before mapping them to A2A.

## References

- [Get Started: Host Your Agent](https://learn.microsoft.com/en-us/agent-framework/get-started/hosting)
- [A2A Integration](https://learn.microsoft.com/en-us/agent-framework/integrations/a2a)
- [Hosting Overview](https://learn.microsoft.com/en-us/agent-framework/agents/hosting)

---

**→ Next: [Module 11 — Persistent Agents](../11-persistent-agents/)**
