# Module 07 — Agents as Tools

**Concept:** Compose agents by using one agent as a function tool for another, enabling specialization and delegation.

## What you'll learn

- How to convert an agent into a tool with `.AsAIFunction()`
- The outer/inner agent pattern (orchestrator + specialist)
- When agent composition is the right approach vs. workflows
- The trade-offs of model-driven routing

## When to use this pattern

Use agents as tools when:
- You want to **delegate a specialized subtask** to a focused agent
- The outer agent should decide **when and whether** to involve the inner agent based on the conversation
- You don't need explicit control over **execution order** — you're fine with the model deciding
- All agents live **in the same process** (for cross-process, see Module 08: A2A)

## How it works

```
Outer agent (orchestrator)
├── Has instructions: "You are a travel planner"
├── Has tool: weatherAgent.AsAIFunction()
│
├── User asks: "What's the weather in Amsterdam?"
│   → Model decides to call the weather agent tool
│   → Weather agent runs independently with its own tools
│   → Weather agent returns: "Cloudy, 15°C"
│   → Outer agent synthesizes and responds
```

`.AsAIFunction()` converts an agent into a regular function tool. From the outer agent's perspective, it's just another tool — it doesn't see the inner agent's reasoning, tool calls, or internal context.

## Why specialized agents beat one giant agent

| Problem with one agent | Solution with composition |
|----------------------|--------------------------|
| Too many tools → poor tool selection | Each agent has only the tools it needs |
| Instructions too broad → focus degrades | Each agent has a tight, focused system prompt |
| Hard to test and maintain | Agents are independently testable units |

## Considerations

| Factor | Details |
|--------|---------|
| **Latency** | Each delegation is a full agent invocation. Keep inner agents focused. |
| **Routing** | The model decides when to call the inner agent. Clear descriptions are critical. |
| **Visibility** | Outer agent sees only the final text response from the inner agent. Use tracing for observability. |
| **Context isolation** | Inner agent doesn't inherit the outer agent's conversation history. |

## Running this module

```bash
cd src/07-agents-as-tools
dotnet run
```

## Anti-patterns to avoid

❌ **Vague inner agent descriptions** — The model uses the description to decide whether to call it. "WeatherAgent" is weak; "An agent that answers weather-related questions including current conditions and forecasts" is better.

❌ **Deep nesting** — Outer → inner → inner-inner chains add compounding latency and make debugging hard.

❌ **Using agents-as-tools when you need guaranteed execution order** — If the sequence matters, use workflows (Module 09).

## References

- [Journey: Agents as Tools](https://learn.microsoft.com/en-us/agent-framework/journey/agents-as-tools)
- [Tools Overview: Using an Agent as a Function Tool](https://learn.microsoft.com/en-us/agent-framework/agents/tools/#using-an-agent-as-a-function-tool)
---

**→ Next: [Module 08 — Agent-to-Agent (A2A)](../08-agent-to-agent/)**
