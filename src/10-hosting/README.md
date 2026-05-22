# Module 10 — Hosting

**Concept:** Expose agents via ASP.NET Core with dependency injection and the A2A protocol so other services (or humans) can call them over HTTP.

## What you'll learn

- How to register agents in DI with `AddAIAgent()`
- How to expose agents via the A2A protocol with `MapA2A()`
- How to expose multiple agents from one host
- How to register and expose workflows
- How to convert a workflow to an AIAgent with `AddAsAIAgent()`
- How A2A `AgentCard` enables agent discovery
- How `contextId` maintains conversation history across HTTP calls

## When to use hosted agents

Use hosting when:
- You want **other services or agents** to call your agent over HTTP
- You need **independent deployability** — your agent runs as its own microservice
- You want to expose your agent to any A2A-compliant client, regardless of language

## Key hosting concepts

### `AddAIAgent()` — the correct hosted pattern

```csharp
// ✅ Correct: registered in DI, can use keyed services, participates in DI lifecycle
var agent = builder.AddAIAgent("weather", instructions: "...", chatClientServiceKey: "chat-model");

// ❌ Wrong for hosting: created inline, not in DI, can't share resources
AIAgent agent = new AIProjectClient(...).AsAIAgent(model: model, instructions: "...");
```

### `MapA2A()` — expose over the A2A protocol

```csharp
app.MapA2A(agent, path: "/a2a/weather", agentCard: new()
{
    Name = "Weather Agent",
    Description = "Answers weather-related questions.",
    Version = "1.0"
});
```

### AgentCard — agent discovery metadata

The AgentCard is returned at `GET /a2a/{name}/v1/card`. Other agents or tools use it to discover what the agent can do before sending messages.

### contextId — conversation continuity

Each A2A request includes a `contextId`. Reuse the same `contextId` across requests to maintain conversation history — this is the HTTP equivalent of `AgentSession`.

### Exposing a workflow

Workflows need to be wrapped as an AIAgent before they can be exposed via A2A:

```csharp
var workflow = builder.AddWorkflow("my-workflow", ...);
var workflowAsAgent = workflow.AddAsAIAgent(); // now exposable via MapA2A
```

## Running this module

```bash
cd src/10-hosting
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://your-project.services.ai.azure.com"
dotnet user-secrets set "AZURE_OPENAI_DEPLOYMENT_NAME" "gpt-4o-mini"
dotnet run
```

Then use `requests.http` with a REST client (VS Code REST Client, JetBrains HTTP Client, or curl) to test the endpoints.

**Available endpoints:**
- `GET /a2a/weather/v1/card` — AgentCard for the weather agent
- `POST /a2a/weather/v1/message:stream` — Call the weather agent
- `POST /a2a/travel/v1/message:stream` — Call the travel agent
- `POST /a2a/trip-planning/v1/message:stream` — Call the sequential workflow

## Credentials best practice

In development, use `dotnet user-secrets` — never `appsettings.json` for sensitive values.

In production:
- Use `ManagedIdentityCredential` instead of `DefaultAzureCredential`
- Read configuration from environment variables, Azure Key Vault, or App Configuration

## Anti-patterns to avoid

❌ **Creating agents inline instead of registering with DI** — use `AddAIAgent()` for hosted scenarios so agents participate in the DI lifecycle.

❌ **Hardcoding credentials** — use `dotnet user-secrets` for development, environment variables or Key Vault for production.

❌ **Exposing a workflow directly without `AddAsAIAgent()`** — workflows don't support A2A natively; wrap them first.

❌ **Ignoring `contextId`** — without a consistent `contextId`, the agent treats each request as a new conversation with no history.

## References

- [Get Started: Host Your Agent](https://learn.microsoft.com/en-us/agent-framework/get-started/hosting)
- [A2A Integration](https://learn.microsoft.com/en-us/agent-framework/integrations/a2a)
- [Hosting Overview](https://learn.microsoft.com/en-us/agent-framework/agents/hosting)
---

**→ Next: [Repo Root](../../README.md)**
