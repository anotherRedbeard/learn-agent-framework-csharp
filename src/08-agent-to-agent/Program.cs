// Module 08: Agent-to-Agent (A2A) — CLIENT SIDE
//
// This module shows how to CALL an agent exposed via the A2A protocol.
// It is the client side of the A2A pattern — Module 10 (Hosting) is the server side.
//
// ⚠️  PREREQUISITE: Module 10 must be running before you run this module.
//     cd ../10-hosting && dotnet run
//     Then come back here and: dotnet run

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

const string baseAddress = "http://localhost:5000";
const string contextId = "a2a-demo-conversation-1"; // reuse this to maintain conversation history

using var http = new HttpClient { BaseAddress = new Uri(baseAddress) };

// --- Step 1: Discover the agent via its AgentCard ---
// The AgentCard is how A2A agents advertise their identity and capabilities.
// Any A2A-compliant client can fetch it to learn what the agent can do.
Console.WriteLine("=== Step 1: Discover the Weather Agent ===");
try
{
    var card = await http.GetFromJsonAsync<JsonElement>("/a2a/weather/v1/card");
    Console.WriteLine($"Agent name:    {card.GetProperty("name")}");
    Console.WriteLine($"Description:   {card.GetProperty("description")}");
    Console.WriteLine($"Version:       {card.GetProperty("version")}");
}
catch (HttpRequestException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Could not reach the A2A server at http://localhost:5000.");
    Console.WriteLine("Make sure Module 10 is running: cd ../10-hosting && dotnet run");
    Console.ResetColor();
    return;
}

// --- Step 2: Send a message to the agent ---
// The A2A message format wraps your text in a structured envelope.
// contextId ties messages to a conversation — reuse it to maintain history.
Console.WriteLine("\n=== Step 2: Send a message to the Weather Agent ===");
var prompt = "What is the weather like in Amsterdam this time of year?";
Console.WriteLine($"> {prompt}");
var response1 = await SendA2AMessage(http, "/a2a/weather/v1/message:stream", prompt, contextId);
Console.WriteLine($"Agent: {response1}");

// --- Step 3: Follow-up in the same conversation ---
// By reusing the same contextId, the agent remembers the previous exchange.
Console.WriteLine("\n=== Step 3: Follow-up (same contextId = same conversation) ===");
var prompt2 = "What should I pack for the weather there?";
Console.WriteLine($"> {prompt2}");
var response2 = await SendA2AMessage(http, "/a2a/weather/v1/message:stream", prompt2, contextId);
Console.WriteLine($"Agent: {response2}");

// --- Step 4: Try the workflow endpoint ---
// Module 10 exposes a sequential workflow: weather agent → travel agent.
// From the client's perspective it looks exactly like a single agent.
Console.WriteLine("\n=== Step 4: Call the sequential workflow (weather → travel) ===");
var prompt3 = "Help me plan a 3-day trip to Amsterdam in October.";
Console.WriteLine($"> {prompt3}");
var response3 = await SendA2AMessage(http, "/a2a/trip-planning/v1/message:stream", prompt3, "trip-planning-demo-1");
Console.WriteLine($"Agent: {response3}");

// Helper: sends an A2A message and returns the agent's text response
static async Task<string> SendA2AMessage(HttpClient http, string path, string text, string contextId)
{
    var payload = new
    {
        message = new
        {
            kind = "message",
            role = "user",
            parts = new[] { new { kind = "text", text, metadata = new { } } },
            messageId = (string?)null,
            contextId
        }
    };

    var httpResponse = await http.PostAsJsonAsync(path, payload);
    httpResponse.EnsureSuccessStatusCode();

    var body = await httpResponse.Content.ReadFromJsonAsync<A2AResponse>();
    return body?.Parts?.FirstOrDefault()?.Text ?? "(no response)";
}

// Minimal model classes for deserializing the A2A response
record A2AResponse(
    [property: JsonPropertyName("parts")] A2APart[]? Parts);

record A2APart(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("text")] string? Text);

