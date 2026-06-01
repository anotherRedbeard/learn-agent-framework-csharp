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

var response = await weatherClient.SendMessageAsync(
    "What is the weather like in Amsterdam this time of year?",
    Role.User,
    contextId);
```

- `A2AHttpJsonClient` is the typed client for the A2A HTTP+JSON binding
- `SendMessageAsync` wraps the protocol envelope (`kind`, `role`, `parts`, `messageId`) for you
- `Role.User` identifies the sender; the response will carry `Role.Agent`
- `contextId` ties multiple requests into one conversation — reuse it for follow-ups

### Extract the reply

```csharp
var message = response.Message
    ?? response.Task?.Status?.Message
    ?? response.Task?.History?.LastOrDefault();

var text = string.Concat(message?.Parts?.Select(p => p.Text) ?? []);
```

- A `SendMessageResponse` may carry either a direct `Message` (synchronous reply) or
  a `Task` (long-running work with status and history) — we handle both
- `Message.Parts` is a list of content parts; `.Text` returns the text payload (empty for non-text parts)

### Call another endpoint the same way

```csharp
var tripPlanningClient = new A2AHttpJsonClient(
    new Uri($"{baseAddress}/a2a/trip-planning"), http);

var response = await tripPlanningClient.SendMessageAsync(
    "Help me plan a 3-day trip to Amsterdam in October.",
    Role.User,
    "trip-planning-demo-1");
```

- The trip-planning endpoint is a **sequential workflow** behind the scenes (weather → travel)
- From the client perspective, it still looks like one A2A agent
- This is the key benefit: implementation details stay behind the protocol boundary

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Tweak an AgentCard's metadata

In Module 10, find the `MapAgentWithCard` helper near the bottom of `Program.cs` and change the Weather Agent's name, description, or version on the `AgentCard` it builds. Restart Module 10, rerun this module, and verify the discovered AgentCard output changed.

> **Why is the card built there and not on `AddAIAgent`?** The current preview of `MapA2AHttpJson` hardcodes `Name = "A2A Agent"` and has no card hook. Module 10 works around this by calling the lower-level `MapHttpA2A(server, card, path)` so we control the card.

### 🟡 Intermediate — Add another A2A agent

Expose a second A2A agent from Module 10, then add a new `GET /card` discovery call and `message:stream` call in this client. Confirm that both agents can be called through the same `HttpClient`.

### 🔴 Stretch — Discover and call an agent via AgentCard

Instead of hardcoding the message endpoint, fetch an AgentCard first and use information from the card to decide which agent to call. Build a tiny routing step that discovers an agent, prints its metadata, then sends a message to the selected endpoint.

> **Hint:** You're building the foundation for multi-agent systems that can discover capabilities dynamically instead of relying on compile-time references.

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
