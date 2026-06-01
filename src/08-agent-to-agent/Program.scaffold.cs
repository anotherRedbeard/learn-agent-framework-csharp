// ============================================================
// Module 08 Scaffold — Agent-to-Agent
// ============================================================
// Use this file to build Module 08 from scratch.
// Copy it over Program.cs, then fill in every TODO.
// Run `dotnet run` after each TODO to see what changed.
//
//   cp Program.scaffold.cs Program.cs
//   dotnet run
//
// ⚠️  PREREQUISITE: Module 10 must be running first.
//     cd ../10-hosting && dotnet run
// ============================================================

// TODO: Add a using for the A2A package.
//       The A2A NuGet package gives you typed clients (A2ACardResolver,
//       A2AHttpJsonClient) so you don't hand-roll the protocol envelope.



// TODO: Set the base address and per-agent paths for the A2A server.
//       Module 10 hosts the server at http://localhost:5000 by default
//       and exposes /a2a/weather and /a2a/trip-planning.
const string baseAddress = "";
const string weatherAgentPath = "";
const string tripPlanningPath = "";

// TODO: Create a stable contextId.
//       Reuse this value across messages that should share conversation history.
const string contextId = "";

// TODO: Create an HttpClient.
//       The typed A2A clients take baseUrls of their own, so this HttpClient
//       does not need a BaseAddress set.
using var http = null!;

// TODO: Discover the Weather Agent by constructing an A2ACardResolver and calling
//       GetAgentCardAsync(). Print the label "=== Step 1: Discover the Weather Agent ==="
//       first, then print card.Name, card.Description, and card.Version.
//
//       URL gotcha: A2ACardResolver joins baseUrl + agentCardPath using Uri rules,
//       so a leading "/" replaces the whole path. Pass the host as baseUrl and
//       the full "/a2a/weather/card" path as agentCardPath.
//
//       If the server cannot be reached, catch HttpRequestException, print a
//       helpful message, and return.
Console.WriteLine("=== Step 1: Discover the Weather Agent ===");

// TODO: Construct an A2AHttpJsonClient pointed at $"{baseAddress}{weatherAgentPath}"
//       and send a message asking about the weather in Amsterdam.
//       Use SendAndExtractText (defined below) to get the reply text.
Console.WriteLine("\n=== Step 2: Send a message to the Weather Agent ===");

// TODO: Send a follow-up message on the same weatherClient using the same contextId.
//       Ask what to pack for the weather there and print the response.
Console.WriteLine("\n=== Step 3: Follow-up (same contextId = same conversation) ===");

// TODO: Construct a second A2AHttpJsonClient pointed at the trip-planning workflow
//       path and send a planning prompt. Use a different contextId for this conversation.
Console.WriteLine("\n=== Step 4: Call the sequential workflow (weather → travel) ===");

// TODO: Write a static helper named SendAndExtractText.
//       Signature: static async Task<string> SendAndExtractText(IA2AClient client, string text, string contextId)
//       Implementation:
//         1. Call client.SendMessageAsync(text, Role.User, contextId).
//         2. The response can carry either a Message (direct reply) or a Task
//            (long-running work). Try response.Message, then response.Task?.Status?.Message,
//            then response.Task?.History?.LastOrDefault().
//         3. If no message or no parts, return "(no response)".
//         4. Otherwise return string.Concat(message.Parts.Select(p => p.Text)).Trim().
