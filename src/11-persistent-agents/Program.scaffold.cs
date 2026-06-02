// ============================================================
// Module 11 Scaffold — Persistent Agents (Foundry Agents v2)
// ============================================================
// Use this file to build Module 11 from scratch.
// Copy it over Program.cs, then fill in every TODO.
// Run `dotnet run` after each TODO to see what changed.
//
//   cp Program.scaffold.cs Program.cs
//   dotnet run
// ============================================================

// TODO: Add using statements for:
//   - Azure.AI.Projects
//   - Azure.AI.Projects.Agents
//   - Azure.Identity
//   - Microsoft.Agents.AI



// TODO: Read AZURE_OPENAI_ENDPOINT from environment variables.
//       Throw a descriptive InvalidOperationException if it's missing.
var endpoint = "";

// TODO: Read AZURE_OPENAI_DEPLOYMENT_NAME from environment variables.
//       Fall back to "gpt-4o-mini" if it's not set.
var deploymentName = "";

const string agentName = "TripBot-Persistent";

// TODO: Create an AIProjectClient.
//       - Point at the endpoint URI
//       - Authenticate with DefaultAzureCredential
//       This is the v2 Foundry client. Agents created via this client show up
//       in the *new* https://ai.azure.com portal under Agents.
AIProjectClient client = null!;

// TODO: Build a Prompt Agent definition.
//       - Call ProjectsAgentDefinition.CreatePromptAgentDefinition(deploymentName)
//         and cast to DeclarativeAgentDefinition
//       - Set Instructions to a brief travel-planning system prompt
DeclarativeAgentDefinition definition = null!;

// TODO: Wrap the definition in a ProjectsAgentVersionCreationOptions.
//       - Optionally set Description
ProjectsAgentVersionCreationOptions options = null!;

// TODO: Grab the admin sub-client (client.AgentAdministrationClient — it's a
//       property, not a method) and create the agent version.
//       - await adminClient.CreateAgentVersionAsync(agentName, options)
AgentAdministrationClient adminClient = null!;
ProjectsAgentVersion version = null!;
Console.WriteLine($"Created agent '{agentName}' (version {version.Version}).");

// TODO: Wrap the server-side agent in the Agent Framework's AIAgent abstraction
//       with client.AsAIAgent(version). This is the same RunAsync /
//       CreateSessionAsync surface used in every earlier module.
AIAgent agent = null!;

// TODO: Create a server-side session with agent.CreateSessionAsync().
//       Unlike Module 03 (in-memory), this session lives in Foundry — the
//       second turn will see the first answer without you re-sending it.
AgentSession session = null!;

// TODO: Send two prompts so you can prove the session is persistent:
//   1. "What are the top 3 things to do in Paris?"
//   2. "How would you change those for a family with young kids?"
//      Print each response.Text.


Console.WriteLine($"\nOpen https://ai.azure.com → your project → Agents to see '{agentName}'.");
Console.WriteLine("Set DELETE_AGENT=true and re-run to clean up automatically.");

// TODO: If the DELETE_AGENT env var is "true", call
//       adminClient.DeleteAgentAsync(agentName).
