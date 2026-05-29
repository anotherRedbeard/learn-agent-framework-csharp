# Module 05 — Middleware

**Concept:** Intercept agent runs and tool calls with reusable middleware — without changing your agent's core logic.

## What you'll learn

- How `.AsBuilder().Use(...)` wraps an agent with middleware
- The difference between run-level middleware and function-invocation middleware
- Why middleware order matters in the pipeline
- How middleware supports logging, telemetry, guardrails, and auditing

## When to use this pattern

Middleware is the right pattern when:
- You need centralized logging or telemetry for every agent run
- You want guardrails that inspect or modify requests before they reach the model
- You need to audit, validate, or short-circuit tool calls
- You want cross-cutting behavior without scattering code across every prompt or tool

---

## Step 1 — Run it first

```bash
cd src/05-middleware
dotnet run
```

You should see TripBot answer a weather question while middleware logs the agent run and the tool invocation. Once it's working, move on to Step 2.

---

## Step 2 — Code walkthrough

Open `Program.cs` and read through it alongside these explanations.

### Create the base agent

```csharp
AIAgent baseAgent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are TripBot, a travel planning assistant.",
        tools: [AIFunctionFactory.Create(GetWeather)]);
```

- The base agent has normal instructions and a weather tool
- `AIFunctionFactory.Create(GetWeather)` exposes a C# method as a callable function
- Middleware is added later, so the agent's core purpose stays clean

> **Why not put logging inside the tool or prompt?** You could — but then every tool and every prompt needs its own logging code. Middleware keeps cross-cutting behavior in one reusable pipeline.

### Add run-level middleware

```csharp
AIAgent agentWithMiddleware = baseAgent
    .AsBuilder()
        .Use(runFunc: LoggingMiddleware, runStreamingFunc: LoggingStreamingMiddleware)
        .Use(CustomFunctionCallingMiddleware)
    .Build();
```

- `.AsBuilder()` starts a configurable agent pipeline
- `.Use(...)` adds middleware layers around the agent
- The first registered middleware is the outermost layer — it sees the request first and the response last
- Providing both `runFunc` and `runStreamingFunc` preserves behavior for non-streaming and streaming calls

### Intercept a full agent run

```csharp
async Task<AgentResponse> LoggingMiddleware(
    IEnumerable<ChatMessage> messages,
    AgentSession? session,
    AgentRunOptions? options,
    AIAgent innerAgent,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"  [Middleware] → Request with {messages.Count()} message(s)");
    var response = await innerAgent.RunAsync(messages, session, options, cancellationToken);
    Console.WriteLine($"  [Middleware] ← Response with {response.Messages.Count} message(s)");
    return response;
}
```

- Run-level middleware wraps the entire `RunAsync` call
- Code before `innerAgent.RunAsync(...)` runs before the model or tools are called
- Code after it returns can inspect or transform the final response
- Returning early would short-circuit the agent run

### Intercept function invocations

```csharp
async ValueTask<object?> CustomFunctionCallingMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"  [Tool Middleware] Calling tool: {context.Function.Name}");
    var result = await next(context, cancellationToken);
    Console.WriteLine($"  [Tool Middleware] Tool result: {result}");
    return result;
}
```

- Function-invocation middleware runs around each tool call
- `context.Function.Name` tells you which tool is about to run
- Calling `next(...)` lets the real tool execute
- You can validate arguments, redact output, or return your own result instead

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Add timestamps to logs

Update both run-level middleware methods so every log line includes the current UTC timestamp. Run it and verify each middleware line shows when it happened.

### 🟡 Intermediate — Redact PII in middleware

Build middleware that strips obvious PII before it reaches the model or before it is logged. Start with simple replacements for email addresses and phone numbers, then test with a prompt that includes both.

> **Hint:** Keep the redaction logic centralized. The goal is to avoid sprinkling privacy cleanup across every tool and every prompt.

### 🔴 Stretch — Estimate tokens and cost

Add middleware that estimates token usage and cost for each run. Start with a rough word-count estimate, then log a projected cost per request before and after the agent responds.

> **Hint:** Exact token counting depends on the model tokenizer. For this exercise, focus on where cost telemetry belongs in the middleware pipeline.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Delete `Program.cs` contents and rebuild from `Program.scaffold.cs`:

```bash
# In src/05-middleware/
cp Program.scaffold.cs Program.cs   # overwrites the solution with the scaffold
dotnet run                           # will fail — that's expected, fill in the TODOs
```

---

## Stretch — Chat-client middleware (the third layer)

There is a **third** middleware extensibility point you haven't seen yet, and it lives one layer below the agent: on the underlying `IChatClient`. The full picture looks like this:

```
[Agent middleware]         ← agent.AsBuilder().Use(...)        wraps RunAsync / RunStreamingAsync
    ↓
[Function middleware]      ← agent.AsBuilder().Use(funcMw)     wraps each tool call
    ↓
[Chat-client middleware]   ← chatClient.AsBuilder().Use(...)   wraps the raw LLM HTTP call
    ↓
[Raw LLM call]
```

The key point: **chat-client middleware is attached to the `IChatClient`, not the agent.** You build it up *before* you construct the agent, then hand the wrapped client over.

```csharp
// Build the chat client with its own middleware pipeline first.
IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsBuilder()
        .UseLogging(loggerFactory)        // built-in: logs raw LLM requests/responses
        .UseOpenTelemetry(...)            // built-in: distributed tracing
        .UseDistributedCache(cache)       // built-in: cache identical LLM calls
        .Use(async (messages, options, innerClient, ct) =>
        {
            var response = await innerClient.GetResponseAsync(messages, options, ct);
            // response.Usage has REAL token counts from the model — no estimation needed.
            Console.WriteLine($"  [ChatClient] tokens in={response.Usage?.InputTokenCount} out={response.Usage?.OutputTokenCount}");
            return response;
        })
    .Build();

// Then create the agent using the already-wrapped client.
AIAgent agent = chatClient.CreateAIAgent(
    instructions: "You are TripBot, a travel planning assistant.",
    tools: [AIFunctionFactory.Create(GetWeather)]);
```

### Where to put what

| Concern | Best layer | Why |
|---|---|---|
| Cost/token tracking (real) | Chat-client | Only this layer sees `response.Usage` from the model |
| Cost estimate before call | Agent | Runs once per turn, before the LLM is hit |
| Tool authorization | Function | Per-call hook with arg/result access |
| Conversation logging | Agent | Sees the full message history and final response |
| Response caching | Chat-client | Caches at the LLM call so identical prompts skip the model |
| Retries on rate-limit | Chat-client | Closest to the HTTP call |
| OpenTelemetry tracing | Chat-client | Built-in `UseOpenTelemetry()` helper |
| Mocking tools in tests | Function | Replace `next(...)` with a fake result |

> **Rule of thumb:** put middleware at the **lowest layer** that has the data it needs. Cost tracking needs real `Usage`, so it belongs on the chat client. Tool authorization needs the function context, so it belongs on the function pipeline. Run-level concerns (full conversation, session state) belong on the agent.

---

## Key concepts

### What middleware intercepts

Middleware can wrap a full agent run, a streaming run, an individual function invocation, or the underlying chat-client call. Use the narrowest interception point that matches the behavior you need.

### What `.AsBuilder().Use(...)` adds

- **Pipeline composition** — stack reusable behaviors around an existing agent
- **Ordering control** — first registered middleware becomes the outermost layer
- **Separation of concerns** — keep logging, guardrails, auditing, and telemetry outside your agent's core logic

### What function-invocation middleware adds

Function middleware gives you a checkpoint around tool execution. That is where validation, audit logging, argument inspection, and tool-result redaction belong.

## Anti-patterns to avoid

❌ **Only providing non-streaming middleware** — streaming will run in non-streaming mode, degrading UX. Always provide both `runFunc` and `runStreamingFunc`.

❌ **Putting cross-cutting concerns in every tool** — logging, redaction, and telemetry should live in middleware so behavior is consistent.

❌ **Changing middleware order casually** — order changes which middleware sees raw input, transformed input, raw output, and transformed output.

## References

- [Middleware Overview](https://learn.microsoft.com/en-us/agent-framework/agents/middleware/)
- [Journey: Adding Middleware](https://learn.microsoft.com/en-us/agent-framework/journey/adding-middleware)

---

**→ Next: [Module 06 — Skills](../06-skills/)**
