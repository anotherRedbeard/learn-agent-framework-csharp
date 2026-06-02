using Azure.AI.AgentServer.Responses;
using Azure.AI.AgentServer.Responses.Models;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Foundry injects AGENT_NAME into the running container so the same image can
// back multiple agent versions. Locally, fall back to a stable name so curl
// requests and the deployed agent.yaml line up.
string agentName = Environment.GetEnvironmentVariable("AGENT_NAME")
    ?? builder.Configuration["AGENT_NAME"]
    ?? "trip-planner";

string endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException(
        "AZURE_OPENAI_ENDPOINT is not set. Use dotnet user-secrets or environment variables.");
string deployment = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
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

// AddFoundryResponses + MapFoundryResponses replace AddAIAgent + MapHttpA2A
// from Module 10. They wire up the Responses-protocol endpoint Foundry calls
// when it routes traffic to this container.
builder.Services.AddFoundryResponses(agent);

// Local-dev only: Foundry normally injects x-agent-user-isolation-key and
// x-agent-chat-isolation-key headers on every request so the hosting layer
// can scope sessions per user/conversation. Without those headers the default
// provider returns null and the request 500s. This fallback supplies stable
// dev keys so dotnet run / docker run work out of the box. In production the
// platform-supplied values flow through context.Isolation untouched.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<HostedSessionIsolationKeyProvider, LocalDevIsolationKeyProvider>();
}

var app = builder.Build();
app.MapFoundryResponses();
app.Run();

internal sealed class LocalDevIsolationKeyProvider : HostedSessionIsolationKeyProvider
{
    public override ValueTask<HostedSessionContext?> GetKeysAsync(
        ResponseContext context, CreateResponse request, CancellationToken cancellationToken)
    {
        var userKey = NonEmpty(context?.Isolation?.UserIsolationKey) ?? "local-dev-user";
        var chatKey = NonEmpty(context?.Isolation?.ChatIsolationKey) ?? "local-dev-chat";
        return new ValueTask<HostedSessionContext?>(new HostedSessionContext(userKey, chatKey));
    }

    private static string? NonEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
