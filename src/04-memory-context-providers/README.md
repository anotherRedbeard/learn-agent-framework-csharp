# Module 04 — Memory & Context Providers

**Concept:** Give your agent memory across turns and inject useful context before each response.

## What you'll learn

- How `AgentSession` keeps related turns connected
- How the default `InMemoryChatHistoryProvider` stores conversation history
- How to implement a custom `ChatHistoryProvider` for persistent memory
- How to use an `AIContextProvider` to inject RAG / profile / dynamic context per turn
- **The key distinction** between session, history provider, and context provider

## The three concepts at a glance

| Concept | Role | Analogy |
|---|---|---|
| `AgentSession` | **Identity** of the conversation thread | A chat thread ID |
| `ChatHistoryProvider` | **Storage** for the thread's transcript (pluggable) | A filing cabinet for chat logs |
| `AIContextProvider` | **Per-turn augmentation** — extras layered on top of the transcript | A research assistant slipping you notes before each meeting |

They are independent. You can use any combination. A long-running personalized agent typically uses **all three**: session to identify the conversation, a custom history provider to persist the transcript, and one or more context providers to inject user profile, RAG hits, and policies on every turn.

## When to use each

| Need | Use |
|---|---|
| Keep a conversation going across turns | Just `AgentSession` (default storage is fine) |
| Survive app restarts; share conversations across instances | Custom `ChatHistoryProvider` |
| Inject user profile / RAG results / runtime tools each call | `AIContextProvider` |
| Long-running agent that learns facts over time | All three |

---

## Step 1 — Run it first

```bash
cd src/04-memory-context-providers
dotnet run
```

You should see three examples in order:

1. **Default in-memory history** — TripBot remembers facts across turns using the built-in provider.
2. **Custom `ChatHistoryProvider`** — same behavior, but every store/provide call is logged so you can see the storage hooks fire.
3. **`AIContextProvider`** — a simulated user-profile lookup runs before every turn and injects preferences as a system message. TripBot then suggests a trip using preferences you never typed.

Once it's working, move on to Step 2.

---

## Step 2 — Code walkthrough

Open `Program.cs` and read through it alongside these explanations.

### Create a session with default memory

```csharp
AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are TripBot...",
        name: "TripBot");

AgentSession session = await agent.CreateSessionAsync();
```

- `AgentSession` ties multiple calls together so the agent can use prior turns as context
- With chat-completion style agents, the framework creates an `InMemoryChatHistoryProvider` automatically
- That default memory is useful for learning, but it disappears when the process exits

### Add facts to memory

```csharp
var prompt = "I prefer window seats and always travel carry-on only.";
Console.WriteLine($"> {prompt}");
Console.WriteLine(await agent.RunAsync(prompt, session));

var prompt2 = "My home airport is Dallas Fort Worth.";
Console.WriteLine($"> {prompt2}");
Console.WriteLine(await agent.RunAsync(prompt2, session));

var prompt3 = "What do you know about my travel preferences so far?";
Console.WriteLine($"> {prompt3}");
Console.WriteLine(await agent.RunAsync(prompt3, session));
```

- Each call passes the same `session`, so the facts accumulate in shared history
- The third call works because the previous user messages are injected as conversation context
- Without the session, each `RunAsync` would start from scratch

### Provide custom history (storage)

```csharp
AIAgent agentWithCustomHistory = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new() { ModelId = deploymentName, Instructions = "You are TripBot..." },
        ChatHistoryProvider = new LoggingChatHistoryProvider(),
        // Foundry's Responses API tracks conversation state server-side. These flags let
        // your custom provider take over instead — no exception, no warning.
        ThrowOnChatHistoryProviderConflict = false,
        WarnOnChatHistoryProviderConflict = false
    });
```

- `ChatClientAgentOptions` lets you replace the default history provider
- `ChatHistoryProvider` is the extension point for storing the transcript in your own backend
- In production, key stored messages by session or user instead of using one shared list

### History provider hooks (store + provide)

```csharp
protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(...)
protected override ValueTask StoreChatHistoryAsync(...)
```

- `ProvideChatHistoryAsync` runs **before** the model call and returns stored messages to inject as transcript
- `StoreChatHistoryAsync` runs **after** the model call and persists the new request and response
- This is the **storage** layer — it owns *the transcript*

### Per-turn context injection (augmentation)

```csharp
AIAgent agentWithContext = new AIProjectClient(...).AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new() { ModelId = deploymentName, Instructions = "..." },
    AIContextProviders = [new UserPreferencesContextProvider()]
});
```

```csharp
class UserPreferencesContextProvider : MessageAIContextProvider
{
    protected override ValueTask<IEnumerable<ChatMessage>> ProvideMessagesAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
    {
        // In production: hit a vector DB, profile store, Mem0, etc.
        var profile = "User profile: prefers window seats, vegetarian, home airport DFW, budget-conscious.";
        return ValueTask.FromResult<IEnumerable<ChatMessage>>(
            [new ChatMessage(ChatRole.System, profile)]);
    }
}
```

- `AIContextProvider` runs **before every invocation** and adds extra messages on top of the transcript
- It does **not** replace storage — `ChatHistoryProvider` still owns the transcript
- This is where RAG, user profiles, memory recall (Mem0), dynamic system prompts, and runtime tool injection live
- The framework stamps these messages so the LLM sees them but they don't pollute the user's transcript

> **Rule of thumb:** If the agent needs *the message history*, use a `ChatHistoryProvider`. If the agent needs *extra info derived elsewhere* (RAG, profile, policy, current time), use an `AIContextProvider`. If the agent should decide for itself when to fetch, use a tool (Module 02).

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Change the canned memory facts

Edit the first example so TripBot learns different travel facts: a preferred airline, a passport detail, or a hotel preference. Run it and verify the final question reflects your new facts.

### 🟡 Intermediate — Make the `AIContextProvider` actually dynamic

The Example 3 provider returns a static profile. Replace it with one that:
1. Reads a JSON file (e.g. `userprofile.json`) on each call
2. Falls back to defaults if the file is missing
3. Logs which fields were loaded

This is the same shape you'd use for a real RAG lookup — swap the file for a vector DB or profile API and you're done.

### 🔴 Stretch — Combine all three (session + history + context)

Build an agent that uses **all three** abstractions together:
1. A session to identify the conversation
2. A `ChatHistoryProvider` persisting messages to JSON or SQLite (keyed by session id)
3. An `AIContextProvider` that summarizes the last 3 stored messages into a short "what we talked about" memo injected as a system message each turn

Stop and restart the app — the agent should still remember and the memo should still get injected.

> **Hint:** Keep memory scoped by session. A single global history list will leak one conversation's context into another.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Delete `Program.cs` contents and rebuild from `Program.scaffold.cs`:

```bash
# In src/04-memory-context-providers/
cp Program.scaffold.cs Program.cs   # overwrites the solution with the scaffold
dotnet run                           # will fail — that's expected, fill in the TODOs
```

---

## Key concepts

### `AgentSession` — conversation identity
A handle that groups related turns. Pass the same session to each `RunAsync` call when the agent should remember previous messages. Think of it as a thread ID.

### `ChatHistoryProvider` — transcript storage
- **Provide history** — inject prior messages before the model runs
- **Store history** — persist the new request and response after the model runs
- **Storage control** — move from process memory to JSON, SQLite, Redis, Cosmos DB, or another backend
- Owns *the transcript itself*

### `AIContextProvider` — per-turn augmentation
- Runs before every invocation
- Returns *additional* messages or system instructions
- Where RAG lookups, user profiles, Mem0 recall, and dynamic tool injection live
- Does **not** replace the transcript — it layers on top

### Tools vs. context providers
- **Tools** are reactive — the LLM decides when to call them (Module 02)
- **Context providers** are proactive — your code runs before every invocation regardless

## Anti-patterns to avoid

❌ **Calling `RunAsync` without a session for memory-dependent flows** — the agent will not have the conversation history you expect.

❌ **Using one global history for every user** — memory must be scoped by session, user, tenant, or another safe boundary.

❌ **Relying on `InMemoryChatHistoryProvider` for production persistence** — it is lost when the process restarts.

## References

- [Get Started: Memory & Persistence](https://learn.microsoft.com/en-us/agent-framework/get-started/memory)
- [Journey: Context Providers](https://learn.microsoft.com/en-us/agent-framework/journey/adding-context-providers)
- [Context Providers reference](https://learn.microsoft.com/en-us/agent-framework/agents/conversations/context-providers)

---

**→ Next: [Module 05 — Middleware](../05-middleware/)**
