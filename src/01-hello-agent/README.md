# Module 01 — Hello Agent

**Concept:** Create your first agent and get a response — in just a few lines of code.

## What you'll learn

- How `AIAgent` wraps an LLM with a persistent identity and instructions
- The difference between non-streaming (`RunAsync`) and streaming (`RunStreamingAsync`)
- Why you use an agent instead of calling the LLM API directly

## When to use this pattern

A basic agent with instructions only is the right starting point when:
- You need a simple Q&A or text-generation assistant
- The LLM's training knowledge is sufficient (no real-time data needed)
- You don't yet need tools, memory, or session management

---

## Step 1 — Run it first

```bash
cd src/01-hello-agent
dotnet run
```

You should see two responses from TripBot — one delivered all at once, one token-by-token. Once it's working, move on to Step 2.

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
- `instructions` are the system prompt — they apply to *every* call this agent makes
- `DefaultAzureCredential` tries `az login`, environment variables, managed identity in order — no API key needed

> **Why not just call the model directly?** You could — but `AIAgent` gives you a reusable, named identity with instructions baked in. Swap the model or endpoint in one place and everything using that agent gets the change.

### Non-streaming call

```csharp
var prompt = "What are the top 3 things to do in Paris?";
Console.WriteLine($"> {prompt}");
Console.WriteLine(await agent.RunAsync(prompt));
```

- `RunAsync` sends the message and **waits** for the complete response before returning
- Use this when you need the full answer before doing something with it (parsing, logging, chaining)

### Streaming call

```csharp
var prompt2 = "Give me a one-sentence travel tip...";
Console.WriteLine($"> {prompt2}");
await foreach (var update in agent.RunStreamingAsync(prompt2))
{
    Console.Write(update);
}
```

- `RunStreamingAsync` returns tokens **as they are generated** — you print each chunk immediately
- Use this for interactive UIs where users shouldn't stare at a blank screen for 3 seconds
- Notice: `await foreach` — this is an async stream (`IAsyncEnumerable<string>`)

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Change TripBot's personality

Edit the `instructions` string. Make TripBot a no-nonsense budget travel expert who always mentions price estimates. Run it and verify the tone change.

### 🟡 Intermediate — Add an interactive prompt

Right now the questions are hardcoded. Replace one of the `RunAsync` calls with this pattern:

```csharp
Console.Write("Ask TripBot: ");
var question = Console.ReadLine()!;
Console.WriteLine(await agent.RunAsync(question));
```

Run it and ask TripBot something unexpected. Does the personality hold?

### 🔴 Stretch — Stream an interactive conversation loop

Replace the entire program body with a `while(true)` loop that:
1. Reads a question from `Console.ReadLine()`
2. Streams the response with `RunStreamingAsync`
3. Exits cleanly when the user types `exit`

> **Hint:** You're building the foundation of Module 03 (multi-turn). Notice that each question here has no memory of the previous one — TripBot forgets everything between calls. Keep that observation in mind.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Delete `Program.cs` contents and rebuild from `Program.scaffold.cs`:

```bash
# In src/01-hello-agent/
cp Program.scaffold.cs Program.cs   # overwrites the solution with the scaffold
dotnet run                           # will fail — that's expected, fill in the TODOs
```

---

## Key concepts

### What a raw LLM call gives you
A stateless request-response. No memory, no tools, no identity. You must manage system prompts, history, and tool orchestration yourself.

### What `AIAgent` adds
- **Instructions** — set once, applied to every call
- **Identity** — a named agent you can reuse and extend
- **Provider abstraction** — swap from Azure OpenAI to any other provider without changing your agent code

## Anti-patterns to avoid

❌ **Hardcoding the endpoint or API key** — use environment variables or `dotnet user-secrets`

❌ **Using `DefaultAzureCredential` in production** — it probes multiple credential sources and adds latency. Use `ManagedIdentityCredential` in production.

## References

- [Get Started: Your First Agent](https://learn.microsoft.com/en-us/agent-framework/get-started/your-first-agent)
- [Journey: From LLMs to Agents](https://learn.microsoft.com/en-us/agent-framework/journey/from-llms-to-agents)

---

**→ Next: [Module 02 — Add Tools](../02-add-tools/)**
