// ============================================================
// Module 09 Scaffold — Workflows
// ============================================================
// Use this file to build Module 09 from scratch.
// Copy it over Program.cs, then fill in every TODO.
// Run `dotnet run` after each TODO to see what changed.
//
//   cp Program.scaffold.cs Program.cs
//   dotnet run
// ============================================================

// TODO: Add using statements for:
//   - Microsoft.Agents.AI
//   - Microsoft.Agents.AI.Workflows



// TODO: Create a deterministic string transformation function.
//       It should convert input text to uppercase.
Func<string, string> uppercaseFunc = null!;

// TODO: Bind the function as an executor named "UppercaseExecutor".
var uppercase = default(Executor<string, string>)!;

// TODO: Create an instance of ReverseTextExecutor.
ReverseTextExecutor reverse = null!;

// TODO: Build a workflow graph that runs uppercase → reverse.
//       - Start WorkflowBuilder with the uppercase executor
//       - Add an edge from uppercase to reverse
//       - Use WithOutputFrom(reverse)
//       - Call Build()
var workflow = default(Workflow)!;

Console.WriteLine("=== Example 1: Deterministic Workflow ===");
Console.WriteLine("Input: 'Paris, France!'");

// TODO: Run the workflow in process with input "Paris, France!".
await using Run run = null!;

// TODO: Loop through run.NewEvents.
//       When an event is ExecutorCompletedEvent, print:
//       "  {executor id}: {data}"


// TODO: Print guidance explaining when workflows are useful.
//       Include examples of known, ordered processes such as:
//       - Research destination → Check weather → Build itinerary
//       - Validate passport → Check visa → Approve


// TODO: Implement ReverseTextExecutor.
//       - Inherit from Executor<string, string>
//       - Give it the id "ReverseTextExecutor"
//       - Override HandleAsync
//       - Return the reversed input string
class ReverseTextExecutor() : Executor<string, string>("ReverseTextExecutor")
{
}
