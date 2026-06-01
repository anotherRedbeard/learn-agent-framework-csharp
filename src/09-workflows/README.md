# Module 09 — Workflows

**Concept:** Orchestrate multi-step processes with explicit, guaranteed execution order.

## What you'll learn

- How `WorkflowBuilder` defines a graph of work
- How `Executor<T>` turns code into workflow steps
- Why workflows are useful when order, branching, or fan-out must be controlled by your code

## When to use this pattern

A workflow is the right pattern when:
- The process structure is known ahead of time
- Each step must run in a predictable order
- You need sequential, branched, or parallel orchestration that should not be left to the model

---

## Step 1 — Run it first

```bash
cd src/09-workflows
dotnet run
```

You should see a deterministic workflow convert `Paris, France!` to uppercase and then reverse it. Once it's working, move on to Step 2.

---

## Step 2 — Code walkthrough

Open `Program.cs` and read through it alongside these explanations.

### Create workflow steps

```csharp
Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

ReverseTextExecutor reverse = new();
```

- `BindAsExecutor` adapts a normal function into a workflow executor
- `ReverseTextExecutor` shows the class-based executor pattern
- Executors are the units of work that pass typed data through the workflow

### Build the workflow graph

```csharp
WorkflowBuilder builder = new(uppercase);
builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
var workflow = builder.Build();
```

- `WorkflowBuilder` starts with the first executor
- `AddEdge` declares the execution path: `uppercase` → `reverse`
- `.WithOutputFrom(reverse)` says the workflow's final output comes from the reverse step

> **Why not just call methods directly?** You could for a two-step demo. Workflows become useful when the graph grows: fixed sequences, branches, fan-out, human gates, retries, and checkpoints all need explicit orchestration.

### Run the workflow

```csharp
var prompt = "Paris, France!";
Console.WriteLine($"> {prompt}");

await using Run run = await InProcessExecution.RunAsync(workflow, prompt);
foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine($"  {executorComplete.ExecutorId}: {executorComplete.Data}");
    }
}
```

- `InProcessExecution.RunAsync` executes the workflow locally
- `Run` contains workflow events produced during execution
- `ExecutorCompletedEvent` lets you inspect each completed step and its output

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Reshape the text an executor passes downstream

In real workflows, the first executor usually doesn't just transform a string — it **shapes the user's raw input into a prompt** that the next executor (often an LLM agent) will consume. The `uppercase` step in this module is a deterministic stand-in for that "prompt builder" role.

Edit the `uppercase` executor so that instead of returning `text.ToUpperInvariant()`, it returns a travel-friendly framing such as:

```csharp
return $"Destination: {text}";
```

Run the workflow and confirm that:

1. The output you see in the console starts with `Destination:` (reversed, since `reverse` still runs after).
2. The `ExecutorCompletedEvent` for `uppercase` shows your new string — proving the value flowing along the edge `uppercase → reverse` is whatever the first executor returned.

The takeaway: each executor's return value **is** the input to the next one. In a real agent workflow you'd swap `reverse` for a `ChatClientAgent` and use this first step to build its prompt.

### 🟡 Intermediate — Add a new workflow step

Add a third executor after `reverse` that appends a short suffix, such as ` [workflow complete]`. Update the `WorkflowBuilder` edges so the flow becomes:

```text
uppercase → reverse → suffix
```

Run it and verify the final output comes from the new step.

### 🔴 Stretch — Add a branch or parallel fan-out

Create two executors that both receive the same input: one formats the text for display and one calculates a simple metric like character count. Wire the workflow so both steps can run from the same earlier node.

> **Hint:** You're practicing the same orchestration choices used in real workflow systems: sequential steps for fixed pipelines, branches for conditional paths, and fan-out when independent work can happen in parallel.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Delete `Program.cs` contents and rebuild from `Program.scaffold.cs`:

```bash
# In src/09-workflows/
cp Program.scaffold.cs Program.cs   # overwrites the solution with the scaffold
dotnet run                           # will fail — that's expected, fill in the TODOs
```

---

## Key concepts

### What a raw sequence of method calls gives you
A simple linear script. It works for small demos, but you own every branching, retry, checkpoint, and inspection concern yourself.

### What workflows add

- **Explicit order** — the graph defines what runs next
- **Typed executors** — each step accepts and returns known data shapes
- **Orchestration options** — sequences, branches, and fan-out are expressed in code

## Anti-patterns to avoid

❌ **Using workflows for every agent task** — try simpler patterns first. Workflows add structure, but they also add code.

❌ **Letting the model choose critical execution order** — if order is mandatory, encode it in the workflow graph.

## References

- [Get Started: Workflows](https://learn.microsoft.com/en-us/agent-framework/get-started/workflows)
- [Journey: Workflows](https://learn.microsoft.com/en-us/agent-framework/journey/workflows)
- [Workflows Overview](https://learn.microsoft.com/en-us/agent-framework/workflows/)

---

**→ Next: [Module 10 — Hosting](../10-hosting/)**
