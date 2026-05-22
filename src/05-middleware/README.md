# Module 05 — Middleware

**Concept:** Intercept and customize agent behavior with a reusable pipeline of cross-cutting concerns.

## What you'll learn

- The three types of middleware in Agent Framework
- How the middleware pipeline (onion model) works
- How to implement logging, guardrails, and tool-call interception
- Why middleware order matters

## When to use middleware

Add middleware when:
- You need **centralized logging or telemetry** for all agent interactions
- You want **guardrails** to block harmful or off-topic content
- You need to **handle exceptions** consistently (retry, fallback)
- You want to **intercept tool calls** for validation or auditing

## Three types of middleware

| Type | What it intercepts | Use for |
|------|-------------------|---------|
| **Agent run middleware** | Full `RunAsync`/`RunStreamingAsync` | Logging, guardrails, retry |
| **Function calling middleware** | Individual tool invocations | Validation, auditing, short-circuiting tools |
| **IChatClient middleware** | Raw chat client calls to the LLM | Request/response transformation |

## The middleware pipeline

```
Caller → [Middleware 1] → [Middleware 2] → Agent core → [Middleware 2] → [Middleware 1] → Caller
```

Each middleware:
1. Runs code **before** delegating (inspect/modify input)
2. Calls the next middleware in the chain (or the agent if last)
3. Runs code **after** the response returns (inspect/modify output)
4. Can **short-circuit** by returning a response without calling next

## Middleware order matters

The **first registered** middleware is the **outermost** layer. It sees the raw input before any other middleware touches it.

## Running this module

```bash
cd src/05-middleware
dotnet run
```

## Anti-patterns to avoid

❌ **Only providing non-streaming middleware** — streaming will run in non-streaming mode, degrading UX. Always provide both `runFunc` and `runStreamingFunc`.

❌ **Expensive operations in middleware** — each middleware layer runs on every request. Calling a remote content moderation API synchronously on every turn adds significant latency.

❌ **Terminating the function call loop carelessly** — setting `context.Terminate = true` in function calling middleware can leave chat history in an inconsistent state.

## References

- [Middleware Overview](https://learn.microsoft.com/en-us/agent-framework/agents/middleware/)
- [Journey: Adding Middleware](https://learn.microsoft.com/en-us/agent-framework/journey/adding-middleware)
---

**→ Next: [Module 06 — Skills](../06-skills/)**
