using Azure.AI.AgentServer.Core;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;

// Foundry injects AGENT_NAME into the running container so the same image can
// back multiple agent versions. Locally, fall back to a stable name so curl
// requests and the deployed manifest line up.
string agentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? "trip-planner";

// In the cloud Foundry injects FOUNDRY_PROJECT_ENDPOINT into the container.
// Locally we use the repo-wide AZURE_OPENAI_ENDPOINT convention; the manifest's
// environment_variables block maps FOUNDRY_PROJECT_ENDPOINT → AZURE_OPENAI_ENDPOINT
// at deploy time so the same code runs in both places.
string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException(
        "AZURE_OPENAI_ENDPOINT is not set. Use dotnet user-secrets or environment variables.");
string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
    ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

// Same agent persona as Module 10's "travel" agent — only the *hosting model*
// changes. Module 10 ran behind A2A on your machine; here, the same agent runs
// inside a Foundry-managed container and speaks the OpenAI Responses protocol.
AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deployment,
        instructions: """
            You are TripBot, a travel itinerary planner. Given a destination and
            trip context, create helpful day-by-day itineraries, suggest activities,
            and provide practical travel tips.
            """,
        name: agentName,
        description: "Creates travel itineraries and trip recommendations.");

// AgentHost.CreateBuilder + RegisterProtocol is the refreshed-preview hosting
// pattern (replaces the older WebApplication.CreateBuilder + app.MapFoundryResponses()
// shape). It lets one container expose multiple protocols (responses, invocations,
// activity) side-by-side; here we only register "responses".
//
// Foundry injects x-agent-user-isolation-key and x-agent-chat-isolation-key
// headers on every request so the hosting layer can scope sessions per
// user/conversation. The default provider reads them straight off the request
// and fails closed (HTTP 500) if they're missing — see requests.http for the
// local-dev header values you need to send.
var builder = AgentHost.CreateBuilder(args);
builder.Services.AddFoundryResponses(agent);
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();
app.Run();
