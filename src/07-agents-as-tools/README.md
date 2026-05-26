# Module 07 — Agents as Tools

**Concept:** Compose agents by turning a specialist agent into a tool another agent can call.

## What you'll learn

- How `.AsAIFunction()` converts an agent into a callable tool
- How an orchestrator agent can delegate work to a specialist sub-agent
- Why composed agents can be easier to maintain than one giant agent
- When to use agent composition instead of explicit workflows

## When to use this pattern

Agents as tools are a good fit when:
- You need a focused sub-agent with its own instructions and tools
- The outer agent should decide when a specialist is needed
- The sub-agent can return a concise result for the outer agent to synthesize
- All agents run in the same process (for cross-process agents, use Module 08)

---

## Step 1 — Run it first

```bash
cd src/07-agents-as-tools
dotnet run
```

You should see TripBot answer travel questions by delegating weather-related work to WeatherAgent. Once it's working, move on to Step 2.

---

## Step 2 — Code walkthrough

Open `Program.cs` and read through it alongside these explanations.

### Connect to the model

```csharp
var client = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
```

- `AIProjectClient` is shared by both agents — they use the same Azure AI endpoint and credentials
- `DefaultAzureCredential` tries `az login`, environment variables, managed identity in order — no API key needed
- The deployment name is read from `AZURE_OPENAI_DEPLOYMENT_NAME`, with `gpt-4o-mini` as the fallback

### Create specialist tools

```csharp
static string GetWeather(string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";

static string GetForecast(string location)
    => $"3-day forecast for {location}: ...";
```

- These are regular C# methods exposed as tools with `AIFunctionFactory.Create(...)`
- The weather agent can call these tools, but the outer TripBot agent cannot call them directly
- `[Description]` attributes help the model understand when and how to use each tool

### Create the specialist agent

```csharp
AIAgent weatherAgent = client.AsAIAgent(
    model: deploymentName,
    instructions: "You are a weather expert...",
    name: "WeatherAgent",
    description: "An agent that answers weather-related questions...",
    tools: [
        AIFunctionFactory.Create(GetWeather),
        AIFunctionFactory.Create(GetForecast)
    ]);
```

- `WeatherAgent` has its own identity, instructions, and tools
- It is intentionally narrow — it only answers weather questions
- The `description` matters because another model will use it to decide whether this agent-tool is relevant

### Turn the specialist into a tool

```csharp
tools: [weatherAgent.AsAIFunction()]
```

- `.AsAIFunction()` wraps an `AIAgent` as a function tool
- From TripBot's point of view, WeatherAgent is just a callable tool
- The outer agent sees the sub-agent's final answer, not every internal tool call or reasoning step

### Create the orchestrator agent

```csharp
AIAgent orchestratorAgent = client.AsAIAgent(
    model: deploymentName,
    instructions: "You are TripBot... For weather-related questions, delegate...",
    tools: [weatherAgent.AsAIFunction()]);
```

- TripBot is the orchestrator — it decides when to call WeatherAgent
- WeatherAgent is the specialist — it handles a focused domain and returns a result
- TripBot synthesizes the specialist result into a final travel recommendation

> **Why not make one giant TripBot with every tool?** You could — but specialist agents keep instructions and tools focused. That usually makes routing easier to explain, test, and maintain.

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Change the sub-agent's instructions

Edit WeatherAgent's `instructions` string. Make it a packing-focused weather advisor that always mentions clothing recommendations. Run it and verify TripBot's final answer changes.

### 🟡 Intermediate — Add a third specialist agent

Create a new specialist agent for local food recommendations. Give it focused instructions, a clear `description`, and expose it to TripBot with `.AsAIFunction()`. Ask TripBot a question that needs both weather and food advice.

### 🔴 Stretch — Route between agents with a classifier

Before calling TripBot, add a lightweight classifier prompt that labels the user's request as `weather`, `food`, or `general`. Use that label to decide which specialist agent-tool TripBot receives.

> **Hint:** This foreshadows Module 09 (workflows). Once you need explicit routing decisions, you may be moving beyond model-driven tool selection.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Delete `Program.cs` contents and rebuild from `Program.scaffold.cs`:

```bash
# In src/07-agents-as-tools/
cp Program.scaffold.cs Program.cs   # overwrites the solution with the scaffold
dotnet run                           # will fail — that's expected, fill in the TODOs
```

---

## Key concepts

### What `.AsAIFunction()` gives you

`.AsAIFunction()` turns an agent into a tool that another agent can call. The inner agent keeps its own instructions, tools, and context boundaries.

### What the orchestrator controls

The orchestrator owns the user-facing response. It chooses whether to call the sub-agent, receives the sub-agent's final text, and synthesizes that into the answer.

### What the sub-agent controls

The sub-agent owns the specialized task. It can use its own tools and instructions without exposing those implementation details to the orchestrator.

## Anti-patterns to avoid

❌ **Vague sub-agent descriptions** — the outer model uses the description to decide whether to call the agent-tool.

❌ **Deep nesting** — orchestrator → specialist → specialist chains add latency and are hard to debug.

❌ **Using agents as tools when order must be guaranteed** — if every step must happen in a fixed sequence, use workflows (Module 09).

## References

- [Journey: Agents as Tools](https://learn.microsoft.com/en-us/agent-framework/journey/agents-as-tools)
- [Tools Overview: Using an Agent as a Function Tool](https://learn.microsoft.com/en-us/agent-framework/agents/tools/#using-an-agent-as-a-function-tool)

---

**→ Next: [Module 08 — Agent-to-Agent (A2A)](../08-agent-to-agent/)**
