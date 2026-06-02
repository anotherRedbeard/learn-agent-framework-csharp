// Scaffold for Module 12 — fill in the TODOs to lift the Module 10 travel agent
// into a Foundry-hosted container.
//
// To use this scaffold:
//   cp Program.scaffold.cs Program.cs   # overwrites the solution
//   dotnet run                          # fails until you implement the TODOs
//
// Reference: completed solution in this folder's git history.

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

var builder = WebApplication.CreateBuilder(args);

// TODO 1: Read AGENT_NAME from the environment (Foundry injects this at runtime).
//         Fall back to a stable local name like "trip-planner" so curl tests work.
string agentName = throw new NotImplementedException("TODO 1");

// TODO 2: Read AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_DEPLOYMENT_NAME from
//         builder.Configuration. Throw a clear InvalidOperationException if either
//         is missing — the container has nothing to talk to without them.
string endpoint = throw new NotImplementedException("TODO 2");
string deployment = throw new NotImplementedException("TODO 2");

// TODO 3: Build the AIAgent.
//         - Construct an AIProjectClient(endpoint, DefaultAzureCredential)
//         - Call .AsAIAgent(model, instructions, name, description)
//         - Use the same TripBot instructions you ported from Module 10's travel agent
AIAgent agent = throw new NotImplementedException("TODO 3");

// TODO 4: Register the agent with Foundry hosting:
//         builder.Services.AddFoundryResponses(agent);

// TODO 5: Build the app and map the Responses endpoint:
//         var app = builder.Build();
//         app.MapFoundryResponses();
//         app.Run();
//
// Note: every request must include x-agent-user-isolation-key and
//       x-agent-chat-isolation-key headers. Foundry injects them in production;
//       see requests.http for the local-dev values to send when testing.
