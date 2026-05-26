// ============================================================
// Module 05 Scaffold — Middleware
// ============================================================
// Use this file to build Module 05 from scratch.
// Copy it over Program.cs, then fill in every TODO.
// Run `dotnet run` after each TODO to see what changed.
//
//   cp Program.scaffold.cs Program.cs
//   dotnet run
// ============================================================

// TODO: Add using statements for:
//   - System.ComponentModel
//   - System.Runtime.CompilerServices
//   - Azure.AI.Projects
//   - Azure.Identity
//   - Microsoft.Agents.AI
//   - Microsoft.Extensions.AI



// TODO: Read AZURE_OPENAI_ENDPOINT from environment variables.
//       Throw a descriptive InvalidOperationException if it's missing.
var endpoint = "";

// TODO: Read AZURE_OPENAI_DEPLOYMENT_NAME from environment variables.
//       Fall back to "gpt-4o-mini" if it's not set.
var deploymentName = "";

// TODO: Create a GetWeather function with a Description attribute.
//       It should accept a location string and return a simple weather response.


// TODO: Create a base AIAgent using AIProjectClient.
//       - Connect to the endpoint with DefaultAzureCredential
//       - Call .AsAIAgent() with a model, instructions, and the weather tool
//       - Instructions should describe TripBot as a travel planning assistant
AIAgent baseAgent = null!;

// TODO: Wrap the base agent with middleware using .AsBuilder().Use(...).Build().
//       Add run-level logging middleware and function-invocation middleware.
AIAgent agentWithMiddleware = null!;

// TODO: Use agentWithMiddleware.RunAsync() to ask TripBot about weather in Paris.
//       Print the label "--- Agent with middleware ---" before the response.
Console.WriteLine("--- Agent with middleware ---");

// TODO: Implement run-level middleware.
//       - Log the number of incoming messages before calling innerAgent.RunAsync(...)
//       - Log the number of response messages after it returns
//       - Return the response


// TODO: Implement streaming middleware.
//       - Use async IAsyncEnumerable<AgentResponseUpdate>
//       - Add [EnumeratorCancellation] to the CancellationToken parameter
//       - Log before streaming starts and after streaming completes
//       - Yield each update from innerAgent.RunStreamingAsync(...)


// TODO: Implement function-invocation middleware.
//       - Log context.Function.Name before calling next(...)
//       - Await next(context, cancellationToken)
//       - Log and return the tool result
