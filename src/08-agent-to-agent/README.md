# Module 08 — Agent-to-Agent

**Concept:** Call agents across process, language, and team boundaries using the A2A protocol over plain HTTP.

## What you'll learn

- How an **AgentCard** advertises an agent's identity, version, and capabilities
- How to send A2A messages with `kind`, `role`, `parts`, `messageId`, and `contextId`
- Why reusing `contextId` gives separate HTTP requests a shared conversation
- Why A2A is useful when another agent is not running in your process or framework

## When to use this pattern

A2A is the right pattern when:
- You need to call an agent exposed by another service, team, or organization
- You want language and framework interoperability over HTTP/JSON
- You need a stable protocol boundary instead of sharing code or SDK types

---

## Step 1 — Run it first

This module is an A2A client. Start the server from Module 10 first, then run this module.

```bash
# Terminal 1 — start the A2A server
cd src/10-hosting
dotnet run

# Terminal 2 — run this A2A client
cd src/08-agent-to-agent
dotnet run
```

You should see the client discover the Weather Agent, send a message, reuse the same `contextId` for a follow-up, and call the trip-planning workflow. Once it's working, move on to Step 2.

---

## Step 2 — Code walkthrough

Open `Program.cs` and read through it alongside these explanations.

### Connect to the A2A server

```csharp
using A2A;

const string baseAddress = "http://localhost:5000";
const string weatherAgentPath = "/a2a/weather";
const string contextId = "a2a-demo-conversation-1";

using var http = new HttpClient();
```

- `baseAddress` points to the service that hosts A2A agents (Module 10)
- The `A2A` package provides typed clients so we don't hand-roll the protocol envelope
- `contextId` is the conversation identifier you reuse across requests

> **Why use the typed client?** A2A is "just JSON over HTTP", but the wire format
> evolves with the spec. `A2ACardResolver` and `A2AHttpJsonClient` keep us in sync
> automatically — and the server in Module 10 uses the same package, so both sides
> always agree.

### Discover the agent

```csharp
var resolver = new A2ACardResolver(
    baseUrl: new Uri(baseAddress),
    httpClient: http,
    agentCardPath: $"{weatherAgentPath}/card");

AgentCard card = await resolver.GetAgentCardAsync();
Console.WriteLine($"Agent name:    {card.Name}");
Console.WriteLine($"Description:   {card.Description}");
Console.WriteLine($"Version:       {card.Version}");
```

- The **AgentCard** tells clients what agent they are calling
- Metadata such as `Name`, `Description`, and `Version` helps clients route or display agents
- Fetching the card first is the A2A equivalent of discovering a service contract

> **URL gotcha:** `A2ACardResolver` joins `baseUrl + agentCardPath` using standard
> `Uri` rules. A leading `/` on the path **replaces** the entire baseUrl path, so we
> pass the host as `baseUrl` and the full `/a2a/weather/card` path as `agentCardPath`.

### Send an A2A message

```csharp
var weatherClient = new A2AHttpJsonClient(
    new Uri($"{baseAddress}{weatherAgentPath}"), http);

var prompt = "What is the weather like in Amsterdam this time of year?";
Console.WriteLine($"> {prompt}");
Console.WriteLine($"Agent: {await SendAndExtractText(weatherClient, prompt, contextId)}");
```

- `A2AHttpJsonClient` is the typed client for the A2A HTTP+JSON binding
- `SendAndExtractText` is a small local helper (defined at the bottom of the file) that hides the request/response plumbing so the main flow stays readable
- `contextId` ties multiple requests into one conversation — reuse it for follow-ups

### Inside the helper: send + extract the reply

```csharp
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
```

- `SendMessageAsync(text, Role.User, contextId)` is the convenience overload that wraps the protocol envelope (`kind`, `role`, `parts`, `messageId`) for you
- `Role.User` identifies the sender; the response will carry `Role.Agent`
- A `SendMessageResponse` may carry either a direct `Message` (synchronous reply) or a `Task` (long-running work with status and history) — we handle both
- `Message.Parts` is a list of content parts; `.Text` returns the text payload (empty for non-text parts)

### Call another endpoint the same way

```csharp
var tripPlanningClient = new A2AHttpJsonClient(
    new Uri($"{baseAddress}{tripPlanningPath}"), http);

var prompt3 = "Help me plan a 3-day trip to Amsterdam in October.";
Console.WriteLine($"> {prompt3}");
Console.WriteLine($"Agent: {await SendAndExtractText(tripPlanningClient, prompt3, "trip-planning-demo-1")}");
```

- The trip-planning endpoint is a **sequential workflow** behind the scenes (weather → travel)
- From the client perspective, it still looks like one A2A agent — same `A2AHttpJsonClient`, same `SendAndExtractText` helper
- This is the key benefit: implementation details stay behind the protocol boundary

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Call the travel agent directly

Module 10 also exposes the travel agent on its own at `/a2a/travel`. Add a new `A2AHttpJsonClient` for it in `Program.cs`, call it once via `SendAndExtractText` (use a fresh `contextId`), and print the reply. You should be able to reuse the same `HttpClient` and the same helper — this proves a single client process can speak to many A2A agents the same way.

> **Hint:** Add a `travelAgentPath` constant near the other path constants and follow the trip-planning pattern at the bottom of `Program.cs`.

### 🟡 Intermediate — Switch one call to streaming

Replace one of your `SendMessageAsync` calls (inside the helper, or in a new helper) with `SendStreamingMessageAsync`. It returns an `IAsyncEnumerable<SendStreamingMessageResponse>` — iterate it with `await foreach` and `Console.Write` text from each event as it arrives so you see the agent type its response in real time.

> **Hint:** Each streaming event mirrors the non-streaming `SendMessageResponse` shape — peek at `evt.Message?.Parts` (or `evt.Task?.Status?.Message?.Parts`) and write any text parts you find.

### 🔴 Stretch — Route to an agent dynamically from its AgentCard

Today the client hardcodes which endpoint matches which user prompt. Make that decision data-driven instead:

1. Use `A2ACardResolver` to fetch all three cards (`/a2a/weather/card`, `/a2a/travel/card`, `/a2a/trip-planning/card`) and store them in a list alongside each agent's base URL.
2. Take a user prompt at the console.
3. Pick the best agent by matching keywords from the prompt against each card's `Name` and `Description` (a simple `string.Contains` heuristic is enough — no LLM needed).
4. Build an `A2AHttpJsonClient` for the chosen agent on the fly and send the message.

> **Hint:** This is the foundation for multi-agent systems that discover capabilities at runtime instead of pinning them at compile time. The same idea scales to a registry of cards fetched from many services.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Delete `Program.cs` contents and rebuild from `Program.scaffold.cs`:

```bash
# In src/08-agent-to-agent/
cp Program.scaffold.cs Program.cs   # overwrites the solution with the scaffold
dotnet run                           # will fail — that's expected, fill in the TODOs
```

---

## Key concepts

### What an AgentCard gives you

A small discovery document for an agent: identity, description, version, and capability metadata. It lets clients understand what they are about to call before sending a message.

### What an A2A message gives you

A protocol-level envelope for agent communication. It separates transport details from agent implementation details so clients do not need to know what framework, model, or workflow is behind the endpoint.

### What `contextId` adds

Conversation continuity across separate HTTP requests. Reuse it when you want follow-up messages to belong to the same remote conversation.

## Anti-patterns to avoid

❌ **Skipping discovery** — hardcoding every assumption about a remote agent makes clients brittle when metadata or capabilities change.

❌ **Using A2A for same-process agents** — if agents live in the same app and team boundary, Module 07's agents-as-tools pattern is usually simpler.

❌ **Changing `contextId` on every turn** — the remote agent will treat each request as a new conversation.

## References

- [Journey: Agent-to-Agent](https://learn.microsoft.com/en-us/agent-framework/journey/agent-to-agent)
- [A2A Integration](https://learn.microsoft.com/en-us/agent-framework/integrations/a2a)
- [A2A Protocol Spec](https://a2a-protocol.org/latest/)

---

**→ Next: [Module 09 — Workflows](../09-workflows/)**
