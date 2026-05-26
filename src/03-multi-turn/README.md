# Module 03 — Multi-Turn Conversations

**Concept:** Keep conversation history across turns so the agent can remember what was said earlier.

## What you'll learn

- How `AgentSession` gives an agent shared conversation state across calls
- How `CreateSessionAsync` starts a stateful conversation
- The difference between passing a session and making stateless `RunAsync` calls

## When to use this pattern

A multi-turn session is the right pattern when:
- Your agent needs to remember details from earlier user messages
- Follow-up questions should build on previous answers
- You need one conversation to stay separate from another conversation

---

## Step 1 — Run it first

```bash
cd src/03-multi-turn
dotnet run
```

You should see three stateful turns from TripBot about a Japan trip, followed by one stateless response that does **not** know the trip context. Once it's working, move on to Step 2.

---

## Step 2 — Code walkthrough

Open `Program.cs` and read through it alongside these explanations.

### Connect to the model

```csharp
AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are TripBot...",
        name: "TripBot");
```

- `AIProjectClient` is your connection to Azure AI — it knows the endpoint and handles auth
- `.AsAIAgent()` wraps that connection into an agent with a **name** and **instructions**
- `instructions` are still the system prompt — they apply to *every* turn in the session
- `DefaultAzureCredential` tries `az login`, environment variables, managed identity in order — no API key needed

### Create a session

```csharp
AgentSession session = await agent.CreateSessionAsync();
```

- `CreateSessionAsync` starts a conversation history for this agent
- `AgentSession` is the handle you pass back on each turn so the agent can see prior messages
- The provider decides where history lives — your app just keeps using the same session object

> **Why not just call `RunAsync` repeatedly?** You can — but without a session, each call is stateless. The agent gets the instructions, but not the previous user messages or answers.

### Stateful turns

```csharp
Console.WriteLine(await agent.RunAsync("I'm planning a 10-day trip to Japan in October...", session));
Console.WriteLine(await agent.RunAsync("What are the must-see places in Kyoto?", session));
Console.WriteLine(await agent.RunAsync("Based on my trip, what should I pack...?", session));
```

- Each `RunAsync` call receives the same `session`
- The second and third turns can refer back to the original Japan trip context
- Use this for chat experiences where users expect natural follow-up questions to work

### Stateless call

```csharp
Console.WriteLine(await agent.RunAsync("What should I pack for my trip?"));
```

- This call does **not** pass the session, so it starts from scratch
- The agent has no information about the Japan trip
- This contrast is the whole point of the module: session = conversation memory, no session = isolated request

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Change the conversation setup

Edit the first message so the user is planning a different trip — for example, a 5-day trip to Iceland in March from Seattle. Run it and verify that the later turns use the new trip context.

### 🟡 Intermediate — Add a `/reset` command

Replace the hardcoded turns with an interactive loop. When the user types `/reset`, create a fresh session:

```csharp
session = await agent.CreateSessionAsync();
Console.WriteLine("Started a new conversation.");
```

Ask follow-up questions before and after `/reset`. Does TripBot forget the old trip?

### 🔴 Stretch — Persist a session transcript to disk

Keep a local transcript of each user message and assistant response, then write it to a file when the user exits. You are not saving the provider's internal session itself — you're saving a readable conversation record your app controls.

> **Hint:** `AgentSession` keeps the active conversation state for the agent. Your transcript file is separate application data for review, debugging, or later import.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Delete `Program.cs` contents and rebuild from `Program.scaffold.cs`:

```bash
# In src/03-multi-turn/
cp Program.scaffold.cs Program.cs && dotnet run   # will fail — that's expected, fill in the TODOs
```

---

## Key concepts

### What `AgentSession` gives you
A shared conversation history. Pass the same session to each call and the agent can use earlier turns as context.

### What `CreateSessionAsync` starts
A new independent conversation. Create a separate session for each user, chat thread, or task flow that should not share history.

### What happens without a session
A stateless request-response. The agent still has its instructions, but it does not know what happened in previous calls.

## Anti-patterns to avoid

❌ **Calling `RunAsync` without a session for conversational flows** — each call will be stateless and the agent will have no memory of the conversation.

❌ **Using one session for multiple unrelated conversations** — sessions accumulate context. Use a separate session per conversation.

## References

- [Get Started: Multi-Turn Conversations](https://learn.microsoft.com/en-us/agent-framework/get-started/multi-turn)
- [Journey: From LLMs to Agents](https://learn.microsoft.com/en-us/agent-framework/journey/from-llms-to-agents)

---

**→ Next: [Module 04 — Memory & Context Providers](../04-memory-context-providers/)**
