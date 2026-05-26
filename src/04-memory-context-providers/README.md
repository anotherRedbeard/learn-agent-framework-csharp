# Module 04 — Memory & Context Providers

**Concept:** Give your agent memory across turns and inject useful context before each response.

## What you'll learn

- How `AgentSession` keeps related turns connected
- How the default `InMemoryChatHistoryProvider` stores conversation history
- How to implement a custom `ChatHistoryProvider` for persistent memory
- Why context providers are proactive, while tools are reactive

## When to use this pattern

Memory and context providers are the right pattern when:
- The agent needs to remember facts from earlier turns in the same conversation
- You need user-specific personalization like preferences, profile details, or account context
- You want to inject information automatically before every model call
- You're preparing for production storage in SQL, Redis, Azure Cosmos DB, or another backend

---

## Step 1 — Run it first

```bash
cd src/04-memory-context-providers
dotnet run
```

You should see TripBot remember travel preferences in one session, then use a custom history provider that logs when history is provided and stored. Once it's working, move on to Step 2.

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

### Provide custom history

```csharp
AIAgent agentWithCustomHistory = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new() { Instructions = "You are TripBot, a travel planning assistant." },
        ChatHistoryProvider = new LoggingChatHistoryProvider()
    });
```

- `ChatClientAgentOptions` lets you replace the default history provider
- `ChatHistoryProvider` is the extension point for storing memory in your own backend
- In production, key stored messages by session or user instead of using one shared list

### Inject context before and after each run

```csharp
protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(...)
protected override ValueTask StoreChatHistoryAsync(...)
```

- `ProvideChatHistoryAsync` runs **before** the model call and returns stored messages to inject
- `StoreChatHistoryAsync` runs **after** the model call and persists the new request and response messages
- This is proactive context: the developer decides what context is always available

> **Rule of thumb:** If the agent should have this information every time, use a context provider. If it should fetch it only when relevant, use a tool.

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Change the canned memory facts

Edit the first example so TripBot learns different travel facts: a preferred airline, a passport detail, or a hotel preference. Run it and verify the final question reflects your new facts.

### 🟡 Intermediate — Implement a custom `IContextProvider`

Add a context provider that injects dynamic context before every run — for example, the current date, the user's loyalty tier, or a small travel policy snippet. Register it with `AIContextProviders` in `ChatClientAgentOptions`, then verify TripBot uses the injected context without being asked directly.

### 🔴 Stretch — Persist memory to JSON or SQLite

Replace the in-memory list in `LoggingChatHistoryProvider` with durable storage:
1. Load messages for the current session in `ProvideChatHistoryAsync`
2. Append the new request and response messages in `StoreChatHistoryAsync`
3. Stop and restart the app, then confirm TripBot still remembers the prior facts

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

### What `AgentSession` gives you
A handle that groups related turns. Pass the same session to each `RunAsync` call when the agent should remember previous messages.

### What `ChatHistoryProvider` adds
- **Provide history** — inject prior messages before the model runs
- **Store history** — persist the new request and response after the model runs
- **Storage control** — move from process memory to JSON, SQLite, Redis, Cosmos DB, or another backend

### Tools vs. context providers
- **Tools** are reactive — the model decides when to call them
- **Context providers** are proactive — your code runs before every invocation
- Use context providers for always-present context: history, profiles, policies, and relevant knowledge

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
