using System.Threading;
using System.Threading.Tasks;
using Azure.AI.AgentServer.Core;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;

// Foundry injects AGENT_NAME into the running container so the same image can
// back multiple agent versions. Locally, fall back to a stable name so curl
// requests and the deployed manifest line up.
string agentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? "trip-planner";

// Foundry auto-injects FOUNDRY_PROJECT_ENDPOINT into hosted containers (and
// `azd ai agent run` sets it locally). For pure `dotnet run` / `docker run`
// flows we fall back to AZURE_OPENAI_ENDPOINT so the rest of the repo's
// convention still works.
string endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException(
        "Neither FOUNDRY_PROJECT_ENDPOINT nor AZURE_OPENAI_ENDPOINT is set.");
string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
    ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

// Pick a credential based on environment.
//
// * `dotnet run` locally → DefaultAzureCredential walks the chain and finds your
//   `az login` token. No extra config needed.
// * `docker run` locally → DefaultAzureCredential has nothing to find inside the
//   container (no az CLI, no managed identity endpoint). The README's Step 3
//   shows you how to mint a short-lived bearer token on the host with
//   `az account get-access-token` and pass it in as AZURE_BEARER_TOKEN. When
//   that env var is present, we use it via a tiny StaticTokenCredential so the
//   container can authenticate without bundling the Azure CLI.
// * In Foundry hosting → Foundry assigns the agent its own managed identity, so
//   DefaultAzureCredential's ManagedIdentityCredential leg picks it up and the
//   AZURE_BEARER_TOKEN escape hatch is never used.
string? bearerToken = Environment.GetEnvironmentVariable("AZURE_BEARER_TOKEN");
TokenCredential credential = string.IsNullOrWhiteSpace(bearerToken)
    ? new DefaultAzureCredential()
    : new StaticTokenCredential(bearerToken!);

// Same agent persona as Module 10's "travel" agent — only the *hosting model*
// changes. Module 10 ran behind A2A on your machine; here, the same agent runs
// inside a Foundry-managed container and speaks the OpenAI Responses protocol.
AIAgent agent = new AIProjectClient(new Uri(endpoint), credential)
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

// Minimal TokenCredential that returns a caller-supplied access token verbatim.
// Useful for local `docker run` where you've minted a short-lived token on the
// host (e.g. via `az account get-access-token`) and want the container to use
// it without installing the Azure CLI or wiring up a service principal.
// Tokens from `az account get-access-token` are valid for ~1 hour — long enough
// for an interactive container test session.
sealed class StaticTokenCredential(string token) : TokenCredential
{
    // We don't know the real expiry of the supplied token; we just have to
    // hand it back when asked. Use 50 minutes as a conservative TTL — slightly
    // less than `az`'s default 60-minute access token lifetime.
    private readonly AccessToken _token = new(token, DateTimeOffset.UtcNow.AddMinutes(50));

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => _token;

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => new(_token);
}
