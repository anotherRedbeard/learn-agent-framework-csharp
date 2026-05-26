// ============================================================
// Module 08 Scaffold — Agent-to-Agent
// ============================================================
// Use this file to build Module 08 from scratch.
// Copy it over Program.cs, then fill in every TODO.
// Run `dotnet run` after each TODO to see what changed.
//
//   cp Program.scaffold.cs Program.cs
//   dotnet run
// ============================================================

// TODO: Add using statements for:
//   - System.Net.Http.Json
//   - System.Text.Json
//   - System.Text.Json.Serialization



// TODO: Set the base address for the A2A server.
//       Module 10 hosts the server at http://localhost:5000 by default.
const string baseAddress = "";

// TODO: Create a stable contextId.
//       Reuse this value across messages that should share conversation history.
const string contextId = "";

// TODO: Create an HttpClient with BaseAddress set to baseAddress.
//       A2A is plain HTTP/JSON, so no Agent Framework packages are required.
using var http = null!;

// TODO: Discover the Weather Agent by fetching its AgentCard from /a2a/weather/v1/card.
//       Print the label "=== Step 1: Discover the Weather Agent ===" first.
//       Print the agent's name, description, and version.
//       If the server cannot be reached, print a helpful message and return.
Console.WriteLine("=== Step 1: Discover the Weather Agent ===");

// TODO: Send a message to the Weather Agent at /a2a/weather/v1/message:stream.
//       Ask about the weather in Amsterdam and print the response.
Console.WriteLine("\n=== Step 2: Send a message to the Weather Agent ===");

// TODO: Send a follow-up message using the same contextId.
//       Ask what to pack for the weather there and print the response.
Console.WriteLine("\n=== Step 3: Follow-up (same contextId = same conversation) ===");

// TODO: Send a message to the trip-planning workflow at /a2a/trip-planning/v1/message:stream.
//       Use a different contextId for this workflow conversation.
Console.WriteLine("\n=== Step 4: Call the sequential workflow (weather → travel) ===");

// TODO: Write a helper method named SendA2AMessage.
//       It should accept HttpClient, path, text, and contextId.
//       It should POST this A2A payload shape:
//       {
//         message: {
//           kind: "message",
//           role: "user",
//           parts: [{ kind: "text", text, metadata: {} }],
//           messageId: null,
//           contextId: contextId
//         }
//       }
//       It should return the first text part from the response, or "(no response)".

// TODO: Add minimal record types for deserializing the A2A response.
//       A2AResponse should expose Parts with JsonPropertyName("parts").
//       A2APart should expose Kind and Text with JsonPropertyName attributes.
