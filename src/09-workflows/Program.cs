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

// --- Example 2: Sequential vs. model-driven ---
// When to use workflows vs. agents-as-tools:
//
// AGENTS AS TOOLS (Module 07):
//   - The MODEL decides which agent to call and when
//   - Good for: flexible, open-ended tasks like "plan my trip"
//   - Trade-off: execution order is non-deterministic
//
// WORKFLOWS:
//   - The GRAPH defines what runs and in what order
//   - Good for: travel pipelines where order matters
//   - Trade-off: more code, less flexibility

Console.WriteLine("\n=== When to use Workflows ===");
Console.WriteLine("Use workflows when the process structure is known ahead of time:");
Console.WriteLine("  ✅ Trip planning: Research destination → Check weather → Build itinerary → Confirm bookings");
Console.WriteLine("  ✅ Travel compliance: Validate passport → Check visa → Verify vaccinations → Approve");
Console.WriteLine("  ✅ Booking pipeline: Search flights (parallel) → Compare prices → Book → Send confirmation");
Console.WriteLine();
Console.WriteLine("Use agents/agents-as-tools when the model should decide:");
Console.WriteLine("  ✅ TripBot that may or may not need weather, visa info, or flight search");
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
