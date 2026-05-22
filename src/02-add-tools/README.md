# Module 02 — Add Tools

**Concept:** Give your agent the ability to act — by calling real functions.

## What you'll learn

- How to define function tools with `[Description]` attributes
- How `AIFunctionFactory.Create` registers a method as a tool
- How the **tool-calling loop** works automatically
- How the model decides which tool to call (and why descriptions matter)

## When to use this pattern

Add tools to your agent when:
- The agent needs **real-time or external data** not in the model's training (live prices, weather, DB records)
- The agent needs to **take actions** (send emails, create tickets, call APIs)

Without tools, an agent is a conversationalist. With tools, it becomes an operator.

## How the tool-calling loop works

```
User asks a question
      ↓
Agent sends messages + tool definitions to LLM
      ↓
LLM decides: call a tool? → yes → Agent Framework executes it
                          → no  → Final response returned
      ↑_______________________________↑
     (loop continues until no more tool calls)
```

You don't write this loop — Agent Framework handles it.

## Why tool descriptions matter

The model selects tools based on their **names and descriptions**. Vague descriptions lead to poor tool selection.

```csharp
// ❌ Vague — model may skip or misuse this
[Description("Does stuff with data.")]

// ✅ Clear — model knows exactly when to call this
[Description("Queries the inventory database for product availability by SKU.")]
```

## Considerations

| Factor | Details |
|--------|---------|
| **Latency** | Each tool call adds a round trip |
| **Token overhead** | Tool definitions consume context tokens — register only what's needed |
| **Reliability** | The model may call tools incorrectly; good descriptions help |
| **Security** | Scope tools to specific, well-defined operations — avoid "run any SQL" |

## Running this module

```bash
cd src/02-add-tools
dotnet run
```

## Anti-patterns to avoid

❌ **Too many tools** — Every tool definition consumes context tokens. An agent that does one thing well beats an agent trying to do everything.

❌ **No error handling** — Tools can fail. Return clear error messages so the model can reason about failures.

❌ **Overly permissive tools** — A tool that can "run any SQL query" is a security risk. Scope to specific operations.

## References

- [Get Started: Add Tools](https://learn.microsoft.com/en-us/agent-framework/get-started/add-tools)
- [Journey: Adding Tools](https://learn.microsoft.com/en-us/agent-framework/journey/adding-tools)
- [Tools Overview](https://learn.microsoft.com/en-us/agent-framework/agents/tools/)
---

**→ Next: [Module 03 — Multi-Turn](../03-multi-turn/)**
