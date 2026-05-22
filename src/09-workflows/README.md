# Module 09 — Workflows

**Concept:** Orchestrate multi-step, multi-agent processes with explicit, guaranteed execution order.

## What you'll learn

- What workflows are and when to use them (vs. simpler patterns)
- The `WorkflowBuilder` and `Executor<T>` building blocks
- How to chain deterministic steps
- The intelligence spectrum — where workflows fit

## ⚠️ Read this first

> "Before reaching for workflows, we recommend you first try simpler patterns to see if they meet your needs. They are easier to set up and debug. Workflows are most useful when you need guaranteed execution order that a single agent can't reliably provide on its own."
> — [Microsoft Docs](https://learn.microsoft.com/en-us/agent-framework/journey/workflows)

## When to use workflows

Use workflows when:
- The **process structure is known ahead of time** (fixed steps, fixed order)
- **Guaranteed execution order** matters (step 2 must always run after step 1)
- You need **human-in-the-loop** gates at defined points in the process
- You need **checkpoints** to resume long-running processes from failures

## The intelligence spectrum

```
Fully intelligent ◄─────────────────────────────────► Fully deterministic
(model decides everything)                            (code decides everything)
     │                         │                         │
     │  Single agent with      │  Workflow with agent    │  Workflow with only
     │  tools                  │  executors              │  deterministic steps
```

## Agents as Tools vs. Workflows

| Question | If "the model" | If "the developer" |
|----------|---------------|-------------------|
| Which subtask next? | Agents as tools | Workflows |
| When to involve another agent? | Agents as tools | Agents in workflows |
| When to ask a human? | Tool approval | Human-in-the-loop gate |
| Handle partial failure? | Retry in tools | Checkpoints |

## Built-in orchestration patterns

Agent Framework includes prebuilt workflow templates:

| Pattern | When to use |
|---------|-------------|
| **Sequential** | Agents execute one after another |
| **Concurrent** | Agents execute in parallel |
| **Handoff** | Agents transfer control based on context |
| **Group Chat** | Agents collaborate in a shared conversation |
| **Magentic** | A manager agent dynamically coordinates specialists |

## Running this module

```bash
cd src/09-workflows
dotnet run
```

## References

- [Get Started: Workflows](https://learn.microsoft.com/en-us/agent-framework/get-started/workflows)
- [Journey: Workflows](https://learn.microsoft.com/en-us/agent-framework/journey/workflows)
- [Workflows Overview](https://learn.microsoft.com/en-us/agent-framework/workflows/)
---

**→ Next: [Module 10 — Hosting](../10-hosting/)**
