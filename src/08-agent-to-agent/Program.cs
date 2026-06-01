// Module 08: Agent-to-Agent (A2A) — CLIENT SIDE
//
// This module shows how to CALL an agent exposed via the A2A protocol.
// It is the client side of the A2A pattern — Module 10 (Hosting) is the server side.
//
// ⚠️  PREREQUISITE: Module 10 must be running before you run this module.
//     cd ../10-hosting && dotnet run
//     Then come back here and: dotnet run

using A2A;

const string baseAddress = "http://localhost:5000";
const string weatherAgentPath = "/a2a/weather";
const string travelAgentPath = "/a2a/travel";
const string tripPlanningPath = "/a2a/trip-planning";

using var http = new HttpClient();

// --- Step 1: Discover available agents via their AgentCards ---
// A2ACardResolver fetches each agent's AgentCard — A2A's discovery document.
// The card metadata lets the client route requests without hardcoding prompt → endpoint.
Console.WriteLine("=== Step 1: Discover available agents ===");
List<DiscoveredAgent> agents;
try
{
    // The default card path is "/.well-known/agent-card.json" but Microsoft's
    // hosting library exposes it at "{agentPath}/card". A2ACardResolver combines
    // baseUrl + agentCardPath using URI rules — a leading "/" replaces the path —
    // so we pass the host as baseUrl and the full path as agentCardPath.
    agents = await DiscoverAgentsAsync(
        new Uri(baseAddress),
        http,
        [weatherAgentPath, travelAgentPath, tripPlanningPath]);

    foreach (var agent in agents)
    {
        Console.WriteLine($"Agent name:    {agent.Card.Name}");
        Console.WriteLine($"Description:   {agent.Card.Description}");
        Console.WriteLine($"Version:       {agent.Card.Version}");
        Console.WriteLine($"Endpoint:      {agent.BaseUrl}");
        Console.WriteLine();
    }
}
catch (HttpRequestException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Could not reach the A2A server at {baseAddress}.");
    Console.WriteLine("Make sure Module 10 is running: cd ../10-hosting && dotnet run");
    Console.ResetColor();
    return;
}

// --- Step 2: Route a user prompt using card metadata ---
// The heuristic is intentionally simple: count prompt keyword matches against
// each card's name and description, then pick the highest-scoring agent.
Console.WriteLine("=== Step 2: Enter a prompt ===");
Console.Write("> ");
var prompt = Console.ReadLine()?.Trim();

if (string.IsNullOrWhiteSpace(prompt))
{
    Console.WriteLine("No prompt provided.");
    return;
}

var selectedAgent = SelectBestAgent(agents, prompt);
var contextId = $"a2a-discovery-{Guid.NewGuid():N}";

Console.WriteLine("\n=== Step 3: Route and send ===");
Console.WriteLine($"Chosen agent:  {selectedAgent.Card.Name}");
Console.WriteLine($"Why:           matched keywords in '{selectedAgent.Card.Name} - {selectedAgent.Card.Description}'");

var client = new A2AHttpJsonClient(selectedAgent.BaseUrl, http);
Console.WriteLine($"Agent: {await SendAndExtractText(client, prompt, contextId)}");

// Helper: sends a text message and extracts the response text.
// SendMessageResponse can carry either a Message (direct reply) or a Task
// (long-running work). For these simple demos we expect a Message.
static async Task<string> SendAndExtractText(IA2AClient client, string text, string contextId)
{
    var response = await client.SendMessageAsync(text, Role.User, contextId);

    var message = response.Message
        ?? response.Task?.Status?.Message
        ?? response.Task?.History?.LastOrDefault();

    if (message?.Parts is null)
    {
        return "(no response)";
    }

    return string.Concat(message.Parts.Select(p => p.Text)).Trim();
}

static async Task<string> SendAndExtractTextStreaming(IA2AClient client, string text, string contextId)
{
    var latest = "";

    await foreach (var update in client.SendStreamingMessageAsync(text, Role.User, contextId))
    {
        var message = update.Message
            ?? update.Task?.Status?.Message
            ?? update.Task?.History?.LastOrDefault();

        if (message?.Parts is not null)
        {
            var chunk = string.Concat(message.Parts.Select(p => p.Text));
            Console.Write(chunk);
            latest += chunk;
        }
    }

    latest = latest.Trim();
    return string.IsNullOrWhiteSpace(latest) ? "(no response)" : latest;
}

static async Task<List<DiscoveredAgent>> DiscoverAgentsAsync(Uri baseUri, HttpClient httpClient, IEnumerable<string> agentPaths)
{
    var discoveredAgents = new List<DiscoveredAgent>();

    foreach (var agentPath in agentPaths)
    {
        var resolver = new A2ACardResolver(
            baseUrl: baseUri,
            httpClient: httpClient,
            agentCardPath: $"{agentPath}/card");

        var card = await resolver.GetAgentCardAsync();
        discoveredAgents.Add(new DiscoveredAgent(card, new Uri(baseUri, agentPath.TrimStart('/'))));
    }

    return discoveredAgents;
}

static DiscoveredAgent SelectBestAgent(IEnumerable<DiscoveredAgent> agents, string prompt)
{
    var normalizedPrompt = prompt.Trim().ToLowerInvariant();

    return agents
        .Select(agent => new
        {
            Agent = agent,
            Score = GetMatchScore(normalizedPrompt, agent.Card),
        })
        .OrderByDescending(result => result.Score)
        .Select(result => result.Agent)
        .First();
}

static int GetMatchScore(string prompt, AgentCard card)
{
    var nameMatches = ExtractKeywords(card.Name)
        .Count(keyword => prompt.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    var descriptionMatches = ExtractKeywords(card.Description)
        .Count(keyword => prompt.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    return (nameMatches * 2) + descriptionMatches;
}

static HashSet<string> ExtractKeywords(string text)
{
    return text
        .Split([' ', '-', ',', '.', ':', ';', '(', ')', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(token => token.Length >= 3)
        .Select(token => token.ToLowerInvariant())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

internal sealed record DiscoveredAgent(AgentCard Card, Uri BaseUrl);
