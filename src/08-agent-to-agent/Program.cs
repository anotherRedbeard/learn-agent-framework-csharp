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
const string tripPlanningPath = "/a2a/trip-planning";
const string contextId = "a2a-demo-conversation-1"; // reuse this to maintain conversation history

using var http = new HttpClient();

// --- Step 1: Discover the agent via its AgentCard ---
// A2ACardResolver fetches the agent's AgentCard — A2A's discovery document.
// The card advertises the agent's identity, capabilities, and skills so any
// A2A-compliant client can decide whether to call it.
Console.WriteLine("=== Step 1: Discover the Weather Agent ===");
AgentCard card;
try
{
    // The default card path is "/.well-known/agent-card.json" but Microsoft's
    // hosting library exposes it at "{agentPath}/card". A2ACardResolver combines
    // baseUrl + agentCardPath using URI rules — a leading "/" replaces the path —
    // so we pass the host as baseUrl and the full path as agentCardPath.
    var resolver = new A2ACardResolver(
        baseUrl: new Uri(baseAddress),
        httpClient: http,
        agentCardPath: $"{weatherAgentPath}/card");
    card = await resolver.GetAgentCardAsync();
    Console.WriteLine($"Agent name:    {card.Name}");
    Console.WriteLine($"Description:   {card.Description}");
    Console.WriteLine($"Version:       {card.Version}");
}
catch (HttpRequestException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Could not reach the A2A server at {baseAddress}.");
    Console.WriteLine("Make sure Module 10 is running: cd ../10-hosting && dotnet run");
    Console.ResetColor();
    return;
}

// --- Step 2: Send a message to the agent ---
// A2AHttpJsonClient is the typed client for the A2A HTTP+JSON binding.
// The SendMessageAsync extension wraps the protocol envelope for plain text messages.
// contextId ties messages to a conversation — reuse it to maintain history.
var weatherClient = new A2AHttpJsonClient(new Uri($"{baseAddress}{weatherAgentPath}"), http);

Console.WriteLine("\n=== Step 2: Send a message to the Weather Agent ===");
var prompt = "What is the weather like in Amsterdam this time of year?";
Console.WriteLine($"> {prompt}");
Console.WriteLine($"Agent: {await SendAndExtractText(weatherClient, prompt, contextId)}");

// --- Step 3: Follow-up in the same conversation ---
// By reusing the same contextId, the agent remembers the previous exchange.
Console.WriteLine("\n=== Step 3: Follow-up (same contextId = same conversation) ===");
var prompt2 = "What should I pack for the weather there?";
Console.WriteLine($"> {prompt2}");
Console.WriteLine($"Agent: {await SendAndExtractText(weatherClient, prompt2, contextId)}");

// --- Step 4: Call the workflow endpoint ---
// Module 10 exposes a sequential workflow: weather agent → travel agent.
// From the client's perspective it looks exactly like a single A2A agent.
var tripPlanningClient = new A2AHttpJsonClient(new Uri($"{baseAddress}{tripPlanningPath}"), http);

Console.WriteLine("\n=== Step 4: Call the sequential workflow (weather → travel) ===");
var prompt3 = "Help me plan a 3-day trip to Amsterdam in October.";
Console.WriteLine($"> {prompt3}");
Console.WriteLine($"Agent: {await SendAndExtractText(tripPlanningClient, prompt3, "trip-planning-demo-1")}");

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
