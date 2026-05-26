# Module 11 — Persistent Agents

**Concept:** Create an agent as a **server-side resource** in Foundry using the Azure AI Agents Service, instead of as a client-side .NET object.

## What you'll learn

- The difference between **client-side agents** (`AIProjectClient.AsAIAgent`) and **persistent agents** (`PersistentAgentsClient`)
- How to create a persistent agent, thread, message, and run
- How to poll a run until it reaches a terminal state
- How to read messages back from a thread
- How persistent agents appear in the Foundry portal

## When to use persistent agents

Use the Azure AI Agents Service when:
- The agent must be **visible and editable** in the Foundry portal
- Multiple apps, languages, or users need to share the **same agent definition**
- You want Foundry-managed **threads, runs, and conversation history** (no local state)
- You need Foundry's **built-in tools** — file search, code interpreter, browser automation

Stick with the **client-side `AIAgent`** (modules 01–10) when:
- The agent's behavior is owned entirely by your code
- You want lightweight, in-process execution with no server-side resources to manage
- You're targeting non-Foundry model providers (OpenAI direct, Anthropic, local models, etc.)

---

## Client-side vs persistent — at a glance

| Aspect | `AIProjectClient.AsAIAgent` (Modules 01–10) | `PersistentAgentsClient` (this module) |
|---|---|---|
| Where it lives | In-memory in your .NET process | Server-side in Foundry |
| Visible in `ai.azure.com` | ❌ No | ✅ Yes (Project → Agents) |
| Conversation state | `AgentSession` (in-process) | `Thread` (server-side) |
| Multi-app sharing | Each app builds its own | One agent, many clients |
| Built-in Foundry tools | ❌ No | ✅ File search, code interpreter, etc. |
| Cost of creating | Free (just a .NET object) | Counts as a Foundry resource |

---

## Step 1 — Run it

```bash
cd src/11-persistent-agents
dotnet run
```

You should see:
1. Created agent ID and thread ID
2. Run status: `Completed`
3. The user prompt and the assistant reply printed back from the thread
4. A note telling you the agent is now visible in the portal

Then go to **[ai.azure.com](https://ai.azure.com)** → your project → **Agents**. You should see `TripBot-Persistent` listed. Click it and you can see its instructions, model deployment, and even chat with it from the portal.

---

## Step 2 — Code walkthrough

### Create the agent

```csharp
var client = new PersistentAgentsClient(endpoint, new DefaultAzureCredential());

PersistentAgent agent = await client.Administration.CreateAgentAsync(
    model: deploymentName,
    name: "TripBot-Persistent",
    instructions: "You are TripBot...");
```

- The `endpoint` is the same project endpoint you've been using since Module 01
- `CreateAgentAsync` writes a real resource to Foundry — you get back an `agent.Id` you can reuse forever

### Create a thread and post a message

```csharp
PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

await client.Messages.CreateMessageAsync(
    threadId: thread.Id,
    role: MessageRole.User,
    content: "What are the top 3 things to do in Paris?");
```

- A **thread** is the server-side analog of `AgentSession` from Module 03 — it holds the conversation history.
- You append messages to the thread, then ask the agent to process them via a run.

### Run the agent and poll

```csharp
ThreadRun run = await client.Runs.CreateRunAsync(thread.Id, agent.Id);
while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
{
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    run = await client.Runs.GetRunAsync(thread.Id, run.Id);
}
```

- A **run** is one invocation of an agent against a thread. It's asynchronous on the server side.
- Terminal statuses: `Completed`, `Failed`, `Cancelled`, `Expired`, `RequiresAction` (tool call needed).

### Read the response

```csharp
await foreach (var message in client.Messages.GetMessagesAsync(thread.Id, order: ListSortOrder.Ascending))
{
    foreach (var item in message.ContentItems)
        if (item is MessageTextContent text)
            Console.WriteLine($"[{message.Role}] {text.Text}");
}
```

- Messages are paged and include both your prompts and the assistant's replies.
- `MessageTextContent` is the common case; persistent agents can also return image, file, and tool-call content items.

---

## Step 3 — Your turn 🛠️

### 🟢 Starter — View the agent in the portal

After running, open **[ai.azure.com](https://ai.azure.com)** → your project → **Agents**. Find `TripBot-Persistent`. Click it, try the **Playground** tab, and chat with the agent directly from the portal. This is the key difference from Module 01.

### 🟡 Intermediate — Reuse the same agent across runs

Right now the program creates a new agent every time. Modify it to:

1. List existing agents with `client.Administration.GetAgentsAsync()`
2. If `TripBot-Persistent` already exists, reuse it instead of creating a new one
3. Otherwise create it

> **Hint:** Real apps look up the agent by name (or store the ID in config) so they don't proliferate duplicates in the project.

### 🔴 Stretch — Multi-turn conversation on the same thread

Wrap the message + run + read loop in a `while(true)` that:

1. Reads a question from the console
2. Posts it to the **same thread** (not a new one)
3. Creates a new run and polls
4. Prints only the latest assistant message

The thread retains the full history server-side — no `AgentSession` needed.

---

## Cleanup

Persistent agents are real resources. To remove them:

**Option A — from this program:**
```bash
DELETE_AGENT=true dotnet run
```

**Option B — from the portal:** Project → Agents → select agent → Delete.

**Option C — via CLI:**
```bash
# List agents (REST via az)
az rest --method get --uri "<your-project-endpoint>/agents?api-version=v1"
```

---

## Anti-patterns to avoid

❌ **Creating a new persistent agent on every app startup** — you'll fill the project with duplicates. Look up by name or store the ID.

❌ **Forgetting to clean up threads** — threads are cheap but accumulate. Delete completed conversation threads or use them as long-lived sessions deliberately.

❌ **Polling without a delay** — `Task.Delay` between `GetRunAsync` calls is required. Tight loops will rate-limit you.

❌ **Mixing client-side and persistent agents in the same project unintentionally** — pick one model per use case. They don't share state.

## References

- [Azure AI Agents Service overview](https://learn.microsoft.com/azure/ai-services/agents/overview)
- [Azure.AI.Agents.Persistent NuGet](https://www.nuget.org/packages/Azure.AI.Agents.Persistent)
- [Foundry portal](https://ai.azure.com)

---

**→ Back to: [Repo Root](../../README.md)**
