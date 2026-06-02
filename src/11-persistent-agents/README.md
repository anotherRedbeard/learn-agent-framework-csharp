# Module 11 — Persistent Agents (Foundry Agents v2)

**Concept:** Create an agent as a **server-side resource** in Foundry using the
new **Agents v2** API, so it shows up in the new [ai.azure.com](https://ai.azure.com)
portal under your project's **Agents** tab — not the legacy *Assistants* surface.

> ℹ️ The earlier `Azure.AI.Agents.Persistent` SDK targets the older **Assistants**
> API. New Foundry projects flag those resources as legacy and ask you to migrate.
> This module uses `Azure.AI.Projects` + `Azure.AI.Projects.Agents` (v2 GA), which
> create **Prompt Agents** in the new experience.

## What you'll learn

- The difference between **client-side agents** (Modules 01–10) and **persistent
  Foundry agents** (this module)
- How to create a Prompt Agent + version with `AIProjectClient.AgentAdministrationClient`
- How to wrap a server-side agent in an `AIAgent` with `client.AsAIAgent(version)`
- How **server-side sessions** preserve conversation history across turns

## When to use this pattern

Use Foundry Agents v2 when:
- The agent must be **visible and editable** in the Foundry portal
- Multiple apps, languages, or users need to share the **same agent definition**
- You want Foundry-managed **versions and conversation history** (no local state)
- You need Foundry's **built-in tools** — file search, code interpreter, etc.

Stick with the **client-side `AIAgent`** (modules 01–10) when:
- The agent's behavior is owned entirely by your code
- You want lightweight, in-process execution with no server-side resources to manage
- You're targeting non-Foundry model providers (OpenAI direct, Anthropic, local models)

---

## Client-side vs persistent — at a glance

| Aspect | Client-side `AIAgent` (Modules 01–10) | Foundry Agent v2 (this module) |
|---|---|---|
| Where it lives | In-memory in your .NET process | Server-side in Foundry |
| Visible in `ai.azure.com` | ❌ No | ✅ Yes (Project → Agents) |
| Conversation state | `AgentSession` (in-process) | `AgentSession` (server-side) |
| Multi-app sharing | Each app builds its own | One agent, many clients |
| Versioning | n/a | Foundry tracks every `CreateAgentVersion` |
| Cost of creating | Free (just a .NET object) | Counts as a Foundry resource |

---

## Step 1 — Run it first

```bash
cd src/11-persistent-agents
dotnet run
```

You should see:
1. `Created agent 'TripBot-Persistent' (version N).`
2. The first prompt and reply about Paris
3. The follow-up about kids — note the answer references Paris without
   you mentioning it again (the session is persistent)
4. A pointer to find it in the portal

Then go to **[ai.azure.com](https://ai.azure.com)** → your project → **Agents**.
You should see `TripBot-Persistent` listed under the **modern** Agents tab
(not "Assistants"). Click it and try the Playground.

---

## Step 2 — Code walkthrough

### Create the project client

```csharp
var client = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
```

- `AIProjectClient` is the v2 entry point. The exact same `endpoint` you've used
  since Module 01 works here.
- Authentication uses `DefaultAzureCredential` — `az login` covers local dev.

### Build a Prompt Agent definition

```csharp
var definition = (DeclarativeAgentDefinition)
    ProjectsAgentDefinition.CreatePromptAgentDefinition(deploymentName);
definition.Instructions = "You are TripBot, a friendly travel planning assistant. Keep answers brief.";
```

- `ProjectsAgentDefinition` is abstract; `CreatePromptAgentDefinition` is the
  factory for the "prompt-only" flavor (no workflow, no hosted code). The portal
  calls this a **Prompt Agent**.
- Set `Instructions` (system prompt), and optionally `Temperature`, `TopP`,
  `Tools`, etc.

### Create the agent version

```csharp
var options = new ProjectsAgentVersionCreationOptions(definition)
{
    Description = "Friendly travel planning assistant",
};

AgentAdministrationClient adminClient = client.AgentAdministrationClient;
ProjectsAgentVersion version = await adminClient.CreateAgentVersionAsync(agentName, options);
```

- `client.AgentAdministrationClient` is a **property** (cached sub-client), not a method.
- Each call to `CreateAgentVersionAsync` produces a new version under the same
  agent name. Foundry shows the version history in the portal.

### Wrap it in the Agent Framework

```csharp
AIAgent agent = client.AsAIAgent(version);
```

- `AsAIAgent` lives in `Microsoft.Agents.AI.Foundry`. It returns a `FoundryAgent`
  (an `AIAgent`) that talks to the server-side resource you just created.
- From here it's the **same `AIAgent` surface** you've been using since Module
  01 — `RunAsync`, `CreateSessionAsync`, streaming overloads, etc.

### Have a multi-turn conversation

```csharp
AgentSession session = await agent.CreateSessionAsync();
foreach (var prompt in new[]
{
    "What are the top 3 things to do in Paris?",
    "How would you change those for a family with young kids?",
})
{
    var response = await agent.RunAsync(prompt, session);
    Console.WriteLine(response.Text);
}
```

- `CreateSessionAsync` on a Foundry-backed agent creates a **server-side session**.
- Compare with Module 03: there, the session was an in-memory list of `ChatMessage`.
  Here it's a Foundry resource — the second prompt sees the first answer without
  you re-sending the history.

---

## Step 3 — Your turn 🛠️

### 🟢 Starter — View the agent in the portal

After running, open **[ai.azure.com](https://ai.azure.com)** → your project →
**Agents**. Find `TripBot-Persistent`. Click it, try the **Playground** tab, and
chat with the agent directly from the portal. Confirm it appears under the
**modern Agents** experience (not "Assistants" / "previously created assistants").
This is the payoff for the SDK switch.

### 🟡 Intermediate — Reuse the agent on subsequent runs

Right now each `dotnet run` creates a new version. Modify the program to:

1. Try `adminClient.GetAgentAsync(agentName)` first
2. If it throws / returns 404, fall through to `CreateAgentVersionAsync`
3. Otherwise call `adminClient.GetAgentVersionAsync(agentName, latestVersion)` and
   reuse it

> **Hint:** Real apps look up agents by name (or store the ID in config) so they
> don't churn versions on every restart.

### 🔴 Stretch — Stream a long answer

Switch from `RunAsync` to the streaming overload:

```csharp
await foreach (var update in agent.RunStreamingAsync("Plan me a 5-day Paris itinerary.", session))
{
    Console.Write(update.Text);
}
```

Notice how tokens arrive incrementally — the Foundry server side is streaming
SSE just like Modules 02 and 10. The same `AIAgent` abstraction works for
in-process and server-side agents.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Replace `Program.cs` with the scaffold:

```bash
cp Program.scaffold.cs Program.cs   # overwrites the solution with the scaffold
dotnet run                          # will fail — that's expected, fill in the TODOs
```

---

## Cleanup

Persistent agents are real resources. To remove them:

**Option A — from this program:**
```bash
DELETE_AGENT=true dotnet run
```

**Option B — from the portal:** Project → Agents → select agent → Delete.

---

## Anti-patterns to avoid

❌ **Creating a new agent / version on every app startup** — you'll fill the
project with duplicate versions. Look up by name first.

❌ **Reaching for `Azure.AI.Agents.Persistent`** — that's the *legacy* Assistants
API. New projects show those as "Assistants are not yet supported" in the new
portal experience.

❌ **Mixing client-side and persistent agents in the same use case** —
they don't share state. Pick one per scenario.

## References

- [Microsoft Foundry Agents overview](https://learn.microsoft.com/azure/foundry/agents/overview)
- [Azure.AI.Projects.Agents NuGet](https://www.nuget.org/packages/Azure.AI.Projects.Agents)
- [Microsoft.Agents.AI.Foundry NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI.Foundry)
- [Foundry portal](https://ai.azure.com)

---

**→ Next: [Module 12 — Foundry-hosted Container](../12-foundry-hosted/)**
