# Module 04 — Memory & Context Providers

**Concept:** Give your agent persistent memory and the ability to inject dynamic context before each response.

## What you'll learn

- How the default `InMemoryChatHistoryProvider` works
- How to implement a custom `ChatHistoryProvider` to persist history to any storage backend
- The difference between **tools** (reactive) and **context providers** (proactive)
- The two-phase lifecycle of context providers: `before_run` and `after_run`

## When to use context providers

Add context providers when:
- The agent needs **conversation history** across turns (most conversational agents)
- You need **user-specific personalization** (profile, preferences, account details)
- You're building **retrieval-augmented generation (RAG)** — automatically injecting relevant docs
- You need **dynamic instructions** that change per invocation (time-based, location-based)

## Tools vs. Context Providers

| Aspect | Tools | Context Providers |
|--------|-------|-------------------|
| **Trigger** | Reactive — model decides when to call | Proactive — runs before every invocation |
| **Control** | Model-driven | Developer-driven |
| **Use case** | On-demand lookups and actions | Always-present context: history, profiles, knowledge |

> **Rule of thumb:** If the agent should have this information _every time_, use a context provider. If it should fetch it _only when relevant_, use a tool.

## Custom ChatHistoryProvider

Implement `ChatHistoryProvider` to store conversation history in any backend:
- SQL database
- Redis / distributed cache
- Azure Cosmos DB
- Any persistent store

This is essential for production — the default `InMemoryChatHistoryProvider` is lost when the process restarts.

## Running this module

```bash
cd src/04-memory-context-providers
dotnet run
```

## References

- [Get Started: Memory & Persistence](https://learn.microsoft.com/en-us/agent-framework/get-started/memory)
- [Journey: Context Providers](https://learn.microsoft.com/en-us/agent-framework/journey/adding-context-providers)
- [Context Providers reference](https://learn.microsoft.com/en-us/agent-framework/agents/conversations/context-providers)
---

**→ Next: [Module 05 — Middleware](../05-middleware/)**
