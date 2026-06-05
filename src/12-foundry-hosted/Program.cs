using Azure.AI.AgentServer.Core;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;

// APPLICATIONINSIGHTS_CONNECTION_STRING is auto-injected in hosted Foundry
// containers. Locally it's optional — without it traces just aren't exported.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    Console.Error.WriteLine(
        "[WARNING] APPLICATIONINSIGHTS_CONNECTION_STRING not set — traces will not be sent " +
        "to Application Insights. (Auto-injected in hosted Foundry containers.)");
}

// Foundry injects AGENT_NAME into the running container so the same image can
// back multiple agent versions. Locally, fall back to a stable name so curl
// requests and the deployed manifest line up.
string agentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? "trip-planner";

// Foundry auto-injects FOUNDRY_PROJECT_ENDPOINT into hosted containers (and
// `azd ai agent run` sets it locally). For plain `dotnet run` we fall back to
// AZURE_OPENAI_ENDPOINT so the rest of the repo's convention still works.
string rawEndpoint =
    Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException(
        "Neither FOUNDRY_PROJECT_ENDPOINT nor AZURE_OPENAI_ENDPOINT is set.");

// Normalize to the canonical Foundry account host and drop any path.
//
// AsAIAgent derives its OpenAI inference URL from this endpoint, and only the
// `<account>.services.ai.azure.com` host routes correctly — the legacy
// `<account>.cognitiveservices.azure.com` alias yields 404 "Project not found"
// through AsAIAgent (even though both aliases answer a direct /openai/v1 call).
// We also strip the /api/projects/<project> path: this account serves the
// Responses route at the account level. Rebuilding from just the account label
// makes the module resilient to either host alias or a stale project-scoped
// value lingering in the shell environment.
var raw = new Uri(rawEndpoint);
string account = raw.Host.Split('.')[0];
var endpoint = new Uri($"https://{account}.services.ai.azure.com");

// The sample's env var is AZURE_AI_MODEL_DEPLOYMENT_NAME; keep the repo's
// AZURE_OPENAI_DEPLOYMENT_NAME as a fallback.
string deployment = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME")
    ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
    ?? throw new InvalidOperationException(
        "Neither AZURE_AI_MODEL_DEPLOYMENT_NAME nor AZURE_OPENAI_DEPLOYMENT_NAME is set.");

// Match the canonical hello-world sample: always DefaultAzureCredential.
//
// * `dotnet run` locally → it walks the chain and finds your `az login` token.
// * In Foundry hosting → the agent's managed identity leg is picked up.
//
// (The previous AZURE_BEARER_TOKEN / StaticTokenCredential branch was removed:
//  a stale or wrong-audience bearer token silently overrode `az login` and was
//  the most likely source of local 401s.)
AIAgent agent = new AIProjectClient(endpoint, new DefaultAzureCredential())
    .AsAIAgent(
        model: deployment,
        instructions: """
            You are TripBot, a travel itinerary planner. Given a destination and
            trip context, create helpful day-by-day itineraries, suggest activities,
            and provide practical travel tips.
            """,
        name: agentName,
        description: "Creates travel itineraries and trip recommendations.");

// AgentHost.CreateBuilder() auto-configures Kestrel on :8088 (or $PORT), the
// GET /readiness probe, OpenTelemetry, and the SSE lifecycle — matching the
// microsoft-foundry hello-world sample.
var builder = AgentHost.CreateBuilder(args);
builder.Services.AddFoundryResponses(agent);
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();
app.Run();
