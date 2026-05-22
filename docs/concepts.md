# Core Concepts

## LLM Fundamentals

Before building agents, it helps to understand what powers them.

A **large language model (LLM)** generates text one token at a time. A token is roughly ¾ of a word — "tokenization" might be split into `["token", "ization"]`. The model predicts the next most likely token given everything before it.

Key things to know as a developer:

| Concept | What it means |
|---------|--------------|
| **Context window** | The maximum tokens the model can "see" at once (input + output). Everything must fit. |
| **Temperature** | Controls randomness. `0` = more deterministic. `0.7+` = more creative. Use low values for agents. |
| **Streaming** | The model generates one token at a time — streaming sends them to the client as they arrive instead of waiting for the full response. |
| **Tool calling** | The model can emit a structured "call this function" output. Your code executes the function and feeds the result back. The model never executes code itself. |
| **Stateless by default** | A raw LLM call has no memory. Each request starts from scratch. Agents solve this. |

> 🎥 Recommended: [Deep Dive into LLMs like ChatGPT](https://www.youtube.com/watch?v=7xTGNNLPyMI) by Andrej Karpathy

---

## AIAgent

The fundamental abstraction. An `AIAgent` wraps an LLM with a persistent identity, instructions, tools, session management, and a middleware pipeline. All agent types derive from this base.

```csharp
AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are a helpful assistant.",
        name: "MyAgent");
```

## AgentSession

Maintains conversation state across multiple turns. Without a session, each `RunAsync` call is stateless.

```csharp
AgentSession session = await agent.CreateSessionAsync();
await agent.RunAsync("Hello, my name is Alice.", session);
await agent.RunAsync("What is my name?", session); // Agent remembers "Alice"
```

## Tools

Functions the agent can call to act on the world. Defined with `[Description]` attributes and registered via `AIFunctionFactory.Create`.

```csharp
[Description("Get the current weather for a location.")]
static string GetWeather([Description("The location")] string location) => $"Sunny in {location}";

AIAgent agent = client.AsAIAgent(model: model, tools: [AIFunctionFactory.Create(GetWeather)]);
```

## Middleware

Cross-cutting pipeline layers that wrap agent runs. Use for logging, guardrails, error handling, and telemetry without changing agent logic.

Three interception points:
- **Agent run middleware** — wraps the full `RunAsync`/`RunStreamingAsync` call
- **Function calling middleware** — wraps individual tool invocations
- **IChatClient middleware** — wraps raw chat client calls to the LLM

## Context Providers

Components that run before and after each agent invocation to inject memory, user profiles, or dynamic context. Unlike tools (reactive), context providers are proactive — they always run.

## Skills

Portable packages of instructions, reference material, and optional scripts. Skills use progressive disclosure: only names/descriptions load upfront (~100 tokens); full content loads on demand.

## Workflows

Explicit graph-based orchestration for multi-step processes where execution order must be guaranteed. Use when "the model decides what happens next" isn't acceptable.

## The Intelligence Spectrum

```
Fully intelligent ◄────────────────────────────────────► Fully deterministic
(model decides everything)                               (code decides everything)
  │                         │                         │
  │  Single agent with      │  Workflow with agent    │  Workflow with only
  │  tools — the model      │  executors — the graph  │  deterministic steps
  │  picks every step       │  controls the process   │  — no LLM involved
```

Use the simplest pattern that meets your requirements.
