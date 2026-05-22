using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

// Workflows give you EXPLICIT control over execution order.
// Use them when "the model decides what happens next" isn't acceptable.
//
// The intelligence spectrum:
//   Fully intelligent (model decides) ◄────────────────► Fully deterministic (code decides)
//   Single agent + tools              Workflow + agents   Workflow + deterministic steps
//
// Workflows are NOT always the right answer.
// Try simpler patterns first: single agent → agents as tools → THEN workflows.

// --- Example 1: Simple deterministic workflow ---
// Step 1: Convert text to uppercase
Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

// Step 2: Reverse the string (class defined at end of file — required by C# top-level programs)
ReverseTextExecutor reverse = new();

// Build the workflow graph: uppercase → reverse
WorkflowBuilder builder = new(uppercase);
builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
var workflow = builder.Build();

Console.WriteLine("=== Example 1: Deterministic Workflow ===");
Console.WriteLine("Input: 'Hello, World!'");

await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");
foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine($"  {executorComplete.ExecutorId}: {executorComplete.Data}");
    }
}

// --- Example 2: Sequential vs. model-driven ---
// When to use workflows vs. agents-as-tools:
//
// AGENTS AS TOOLS (Module 07):
//   - The MODEL decides which agent to call and when
//   - Good for: flexible, open-ended tasks
//   - Trade-off: execution order is non-deterministic
//
// WORKFLOWS:
//   - The GRAPH defines what runs and in what order
//   - Good for: document review pipelines, onboarding flows, approval chains
//   - Trade-off: more code, less flexibility

Console.WriteLine("\n=== When to use Workflows ===");
Console.WriteLine("Use workflows when the process structure is known ahead of time:");
Console.WriteLine("  ✅ Document review: Write → Review → Revise → Approve (must be in order)");
Console.WriteLine("  ✅ Onboarding: Collect info → Compliance check → Provision → Notify");
Console.WriteLine("  ✅ Analytics: Gather data (parallel) → Merge → Generate report");
Console.WriteLine();
Console.WriteLine("Use agents/agents-as-tools when the model should decide:");
Console.WriteLine("  ✅ Q&A assistant that may or may not need to look up weather");
Console.WriteLine("  ✅ Travel planner that delegates to specialists based on the request");

// --- Type declarations must come after all top-level statements in C# ---
// In a top-level program, class definitions are placed at the bottom of the file.
// The class can still be instantiated above (C# resolves forward references for types).

class ReverseTextExecutor() : Executor<string, string>("ReverseTextExecutor")
{
    public override ValueTask<string> HandleAsync(
        string message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(string.Concat(message.Reverse()));
    }
}
