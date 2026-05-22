# Module 03 — Multi-Turn Conversations

**Concept:** Maintain conversation state across multiple turns so the agent remembers what was said.

## What you'll learn

- What `AgentSession` is and why it matters
- The difference between stateless and stateful agent calls
- How to maintain context in a conversation

## When to use this pattern

Use sessions whenever your agent needs to:
- Remember context from previous turns in the conversation
- Build on what the user said earlier
- Maintain any state across multiple interactions

Almost all useful conversational agents need sessions.

## How it works

```
AgentSession session = await agent.CreateSessionAsync();

// Each call with the session appends to shared conversation history
await agent.RunAsync("My name is Alice", session);   // turn 1
await agent.RunAsync("What's my name?", session);    // turn 2 — agent remembers "Alice"
```

Without a session, each `RunAsync` starts from scratch — no memory of previous turns.

## Session management

Different providers handle session storage differently:
- **Foundry Agent Service** — chat history is stored on the service side
- **Chat Completion / Responses** — history is stored in memory (`InMemoryChatHistoryProvider`) automatically

The session abstracts this difference. Your code is the same regardless of provider.

## Running this module

```bash
cd src/03-multi-turn
dotnet run
```

## Anti-patterns to avoid

❌ **Calling `RunAsync` without a session for conversational flows** — each call will be stateless and the agent will have no memory of the conversation.

❌ **Using one session for multiple unrelated conversations** — sessions accumulate context. Use a separate session per conversation.

## References

- [Get Started: Multi-Turn Conversations](https://learn.microsoft.com/en-us/agent-framework/get-started/multi-turn)
- [Journey: From LLMs to Agents](https://learn.microsoft.com/en-us/agent-framework/journey/from-llms-to-agents)
---

**→ Next: [Module 04 — Memory](../04-memory-context-providers/)**
