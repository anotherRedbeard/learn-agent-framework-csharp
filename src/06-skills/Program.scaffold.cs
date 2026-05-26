// ============================================================
// Module 06 Scaffold — Skills
// ============================================================
// Use this file to build Module 06 from scratch.
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

// TODO: Create an AgentSkillsProvider that loads skills from the local skills directory.
//       Use Path.Combine(AppContext.BaseDirectory, "skills").
AgentSkillsProvider skillsProvider = null!;

// TODO: Create an AIAgent using AIProjectClient.
//       - Connect to the endpoint with DefaultAzureCredential
//       - Call .AsAIAgent() with ChatClientAgentOptions
//       - Name the agent TripBot
//       - Give it travel-planning instructions focused on entry requirements
//       - Add skillsProvider to AIContextProviders
AIAgent agent = null!;

// TODO: Create an AgentSession so follow-up questions share context.
AgentSession session = null!;

// TODO: Use agent.RunAsync() to ask whether a US citizen needs a visa for Japan.
//       Pass the session and print the response.


// TODO: Ask a follow-up question about visiting France and Italy on the same trip.
//       Pass the same session and print the response.

