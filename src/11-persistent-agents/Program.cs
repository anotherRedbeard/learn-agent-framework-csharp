using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
const string agentName = "TripBot-Persistent";

// AIProjectClient targets the *new* Foundry projects API (v2). Agents created
// through client.Agents.* are Prompt Agents — they show up in the new
// https://ai.azure.com portal under your project's "Agents" tab.
//
// Contrast this with Azure.AI.Agents.Persistent (the old Assistants API): that
// SDK creates "Assistants" which the new Foundry experience flags as legacy
// and asks you to migrate.
var client = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());

// 1. Build a Prompt Agent definition. ProjectsAgentDefinition is the abstract
//    base; CreatePromptAgentDefinition is the factory for the "prompt-only"
//    flavor (no workflow, no hosted code), which is what the portal calls a
//    "Prompt agent".
var definition = (DeclarativeAgentDefinition)
    ProjectsAgentDefinition.CreatePromptAgentDefinition(deploymentName);
definition.Instructions = "You are TripBot, a friendly travel planning assistant. Keep answers brief.";

var options = new ProjectsAgentVersionCreationOptions(definition)
{
    Description = "Friendly travel planning assistant",
};

// 2. Create the agent as a server-side resource. Each call produces a new
//    version — Foundry tracks the version history for you.
AgentAdministrationClient adminClient = client.AgentAdministrationClient;
ProjectsAgentVersion version = await adminClient.CreateAgentVersionAsync(agentName, options);
Console.WriteLine($"Created agent '{agentName}' (version {version.Version}).");

// 3. Wrap the Foundry-side agent in the Agent Framework's AIAgent abstraction.
//    This is the same surface used in every earlier module — RunAsync,
//    CreateSessionAsync, streaming — but conversations and history now live
//    in Foundry instead of in the local process.
AIAgent agent = client.AsAIAgent(version);

// 4. Have a two-turn conversation. The session is server-side, so the second
//    prompt sees the first answer even though we never re-send it.
AgentSession session = await agent.CreateSessionAsync();
foreach (var prompt in new[]
{
    "What are the top 3 things to do in Paris?",
    "How would you change those for a family with young kids?",
})
{
    Console.WriteLine($"\n> {prompt}");
    var response = await agent.RunAsync(prompt, session);
    Console.WriteLine(response.Text);
}

Console.WriteLine($"\nOpen https://ai.azure.com → your project → Agents to see '{agentName}'.");
Console.WriteLine("Set DELETE_AGENT=true and re-run to clean up automatically.");

if (string.Equals(Environment.GetEnvironmentVariable("DELETE_AGENT"), "true", StringComparison.OrdinalIgnoreCase))
{
    await adminClient.DeleteAgentAsync(agentName);
    Console.WriteLine($"Deleted agent '{agentName}'.");
}
