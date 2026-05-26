// ============================================================
// Module 01 Scaffold — Hello Agent
// ============================================================
// Use this file to build Module 01 from scratch.
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

// TODO: Use agent.RunAsync() to ask TripBot a travel question.
//       Print the label "--- Non-streaming ---" before the response.
Console.WriteLine("--- Non-streaming ---");

// TODO: Use agent.RunStreamingAsync() with await foreach to stream a second question.
//       Print each chunk with Console.Write() (no newline between chunks).
//       Print the label "--- Streaming ---" before starting.
Console.WriteLine("\n--- Streaming ---");
Console.WriteLine(); // final newline after streaming
