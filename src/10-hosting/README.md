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

```bash
cd src/10-hosting
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://your-project.services.ai.azure.com"
dotnet user-secrets set "AZURE_OPENAI_DEPLOYMENT_NAME" "gpt-4o-mini"
dotnet run
```

Then open `requests.http` and send the AgentCard and message requests. You should see hosted endpoints for the weather agent, travel agent, and trip-planning workflow.

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
    chatClientServiceKey: "chat-model");
```

- `AddAIAgent()` registers the agent in dependency injection instead of creating it inline
- The first argument becomes the agent key used by DI and hosting
- `instructions` define the agent's behavior for every request
- `description` becomes part of the hosted agent metadata

> **Why not create the agent directly?** Hosted agents should participate in the ASP.NET Core DI lifecycle. `AddAIAgent()` lets agents share configured services, keyed clients, and deployment-time configuration.

### Register a workflow as an agent

```csharp
var planningWorkflow = builder.AddWorkflow("trip-planning", (sp, key) =>
{
    var weather = sp.GetRequiredKeyedService<AIAgent>("weather");
    var travel = sp.GetRequiredKeyedService<AIAgent>("travel");
    return AgentWorkflowBuilder.BuildSequential(key, [weather, travel]);
});

var planningWorkflowAsAgent = planningWorkflow.AddAsAIAgent();
```

- `AddWorkflow()` registers a workflow that composes multiple agents
- `GetRequiredKeyedService<AIAgent>()` resolves the hosted agents from DI
- `BuildSequential()` sends the request through weather first, then travel
- `AddAsAIAgent()` wraps the workflow so it can be exposed through A2A like any other agent

### Expose A2A endpoints

```csharp
app.MapA2AHttpJson(weatherAgent, "/a2a/weather");
app.MapA2AHttpJson(travelAgent, "/a2a/travel");
app.MapA2AHttpJson(planningWorkflowAsAgent, "/a2a/trip-planning");
```

- `MapA2AHttpJson()` maps A2A HTTP-JSON routes for each hosted agent — this is the `MapA2A` hosting step in this module
- `GET /a2a/weather/v1/card` returns the weather agent's `AgentCard`
- `POST /a2a/weather/v1/message:stream` sends a message to the weather agent
- A2A clients use the `AgentCard` to discover the agent before sending messages

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

Edit one hosted agent's `description` in `Program.cs`. Run the app, request that agent's card from `requests.http`, and verify the metadata changed.

### 🟡 Intermediate — Add a new hosted agent

Register a third agent with `AddAIAgent()` and expose it with `MapA2AHttpJson()` at its own endpoint, such as `/a2a/packing`. Add matching requests to `requests.http` and verify its `AgentCard`, `contextId`, and message endpoint work.

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

❌ **Exposing a workflow directly** — wrap workflows with `AddAsAIAgent()` before mapping them to A2A.

## References

- [Get Started: Host Your Agent](https://learn.microsoft.com/en-us/agent-framework/get-started/hosting)
- [A2A Integration](https://learn.microsoft.com/en-us/agent-framework/integrations/a2a)
- [Hosting Overview](https://learn.microsoft.com/en-us/agent-framework/agents/hosting)

---

**→ Next: [Module 11 — Persistent Agents](../11-persistent-agents/)**
