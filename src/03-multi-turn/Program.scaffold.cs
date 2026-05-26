// ============================================================
// Module 03 Scaffold — Multi-Turn Conversations
// ============================================================
// Use this file to build Module 03 from scratch.
// Copy it over Program.cs, then fill in every TODO.
// Run `dotnet run` after each TODO to see what changed.
//
//   cp Program.scaffold.cs Program.cs
//   dotnet run
// ============================================================

// TODO: Add using statements for:
//   - Azure.AI.Projects
//   - Azure.Identity
//   - Microsoft.Agents.AI



// TODO: Read AZURE_OPENAI_ENDPOINT from environment variables.
//       Throw a descriptive InvalidOperationException if it's missing.
var endpoint = "";

// TODO: Read AZURE_OPENAI_DEPLOYMENT_NAME from environment variables.
//       Fall back to "gpt-4o-mini" if it's not set.
var deploymentName = "";

// TODO: Create an AIAgent using AIProjectClient.
//       - Connect to the endpoint with DefaultAzureCredential
//       - Call .AsAIAgent() with a model, instructions, and name
//       - Instructions should describe TripBot as a travel planning assistant
AIAgent agent = null!;

// TODO: Create an AgentSession with agent.CreateSessionAsync().
//       This session should be reused across multiple turns.
AgentSession session = null!;

// TODO: Turn 1 — use agent.RunAsync() with the session to establish trip context.
//       Print the label "Turn 1:" before the response.
Console.WriteLine("Turn 1:");

// TODO: Turn 2 — ask a follow-up question using the same session.
//       Print the label "Turn 2:" before the response.
Console.WriteLine("\nTurn 2:");

// TODO: Turn 3 — ask another follow-up question using the same session.
//       Print the label "Turn 3:" before the response.
Console.WriteLine("\nTurn 3:");

// TODO: Make one final RunAsync() call without passing the session.
//       Print the label "--- Without session (stateless) ---" before the response.
//       Notice that the agent no longer knows the trip details.
Console.WriteLine("\n--- Without session (stateless) ---");
