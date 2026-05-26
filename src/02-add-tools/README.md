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

---

## Step 1 — Run it first

```bash
cd src/02-add-tools
dotnet run
```

You should see TripBot answer travel questions by using the weather and time tools. One response asks about Amsterdam weather; the next may call multiple tools for Tokyo weather and local time. Once it's working, move on to Step 2.

---

## Step 2 — Code walkthrough

Open `Program.cs` and read through it alongside these explanations.

### Connect to the model

```csharp
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
```

- `AZURE_OPENAI_ENDPOINT` tells `AIProjectClient` where to connect
- `AZURE_OPENAI_DEPLOYMENT_NAME` selects the model deployment, with a local default for quick starts
- Keeping configuration in environment variables avoids hardcoding deployment details in source

### Define tools as regular methods

```csharp
[Description("Get the current weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";

[Description("Get the current time in a given city.")]
static string GetTime([Description("The city to get the time for.")] string city)
    => $"The current time in {city} is 14:32 local time.";
```

- Tools are just C# methods — no special base class required
- `[Description]` tells the model when to call the tool and what each argument means
- The model selects tools based on their **names and descriptions**, so vague descriptions lead to poor tool selection

### Register tools with the agent

```csharp
AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are TripBot...",
        tools: [
            AIFunctionFactory.Create(GetWeather),
            AIFunctionFactory.Create(GetTime)
        ]);
```

- `AIFunctionFactory.Create` converts a C# method into a tool definition the model can see
- `tools` gives the agent capabilities beyond text generation
- `DefaultAzureCredential` tries `az login`, environment variables, managed identity in order — no API key needed
- Register only the tools this agent needs; every tool definition consumes context tokens

### Let the framework handle the tool-calling loop

```csharp
var prompt = "I'm heading to Amsterdam next week. What's the weather like there?";
Console.WriteLine($"> {prompt}");
Console.WriteLine(await agent.RunAsync(prompt));
```

- You don't call `GetWeather` yourself — the model decides when the question requires it
- Agent Framework automatically executes the function, sends the result back to the model, and returns the final answer
- Use this when the agent needs real-time or external data not in the model's training

### Allow multiple tool calls

```csharp
var prompt2 = "I'm flying to Tokyo tomorrow. What's the weather and local time there right now?";
Console.WriteLine($"> {prompt2}");
Console.WriteLine(await agent.RunAsync(prompt2));
```

- A single user message can require more than one tool call
- The loop continues until the model no longer asks for tools and returns a final response
- Each tool call adds latency, so keep tools focused and useful

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Rename a tool description

Edit the `[Description]` on `GetWeather` to be more specific about trip planning, then run it and verify TripBot still calls the weather tool.

### 🟡 Intermediate — Add a packing tool

Add a new `GetPackingTip` tool that takes a destination and returns one packing suggestion. Register it with `AIFunctionFactory.Create`, then ask TripBot what to pack for Amsterdam.

### 🔴 Stretch — Watch tool selection

Give TripBot three or more focused tools, then ask mixed questions that should use one, several, or none of them. Compare how changing method names and `[Description]` text affects selection.

> **Hint:** You're building toward Module 03 (multi-turn). Today each call is independent; next you'll keep context across turns.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Delete `Program.cs` contents and rebuild from `Program.scaffold.cs`:

```bash
# In src/02-add-tools/
cp Program.scaffold.cs Program.cs   # overwrites the solution with the scaffold
dotnet run                           # will fail — that's expected, fill in the TODOs
```

---

## Key concepts

### How the tool-calling loop works

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

### Why tool descriptions matter

The model selects tools based on their **names and descriptions**. Vague descriptions lead to poor tool selection.

```csharp
// ❌ Vague — model may skip or misuse this
[Description("Does stuff with data.")]

// ✅ Clear — model knows exactly when to call this
[Description("Queries the inventory database for product availability by SKU.")]
```

### Tool trade-offs

| Factor | Details |
|--------|---------|
| **Latency** | Each tool call adds a round trip |
| **Token overhead** | Tool definitions consume context tokens — register only what's needed |
| **Reliability** | The model may call tools incorrectly; good descriptions help |
| **Security** | Scope tools to specific, well-defined operations — avoid "run any SQL" |

## Anti-patterns to avoid

❌ **Too many tools** — Every tool definition consumes context tokens. An agent that does one thing well beats an agent trying to do everything.

❌ **Vague tool descriptions** — The model may skip or misuse a tool when the name and `[Description]` don't explain when to call it.

❌ **No error handling** — Tools can fail. Return clear error messages so the model can reason about failures.

❌ **Overly permissive tools** — A tool that can "run any SQL query" is a security risk. Scope to specific operations.

## References

- [Get Started: Add Tools](https://learn.microsoft.com/en-us/agent-framework/get-started/add-tools)
- [Journey: Adding Tools](https://learn.microsoft.com/en-us/agent-framework/journey/adding-tools)
- [Tools Overview](https://learn.microsoft.com/en-us/agent-framework/agents/tools/)

---

**→ Next: [Module 03 — Multi-Turn](../03-multi-turn/)**
