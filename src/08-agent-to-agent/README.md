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
const string baseAddress = "http://localhost:5000";
const string contextId = "a2a-demo-conversation-1";

using var http = new HttpClient { BaseAddress = new Uri(baseAddress) };
```

- `baseAddress` points to the service that hosts A2A agents
- `HttpClient` is enough — this module does not need Agent Framework packages
- `contextId` is the conversation identifier you reuse across requests

> **Why plain HTTP?** A2A is designed for interoperability. A .NET client, Python client, shell script, or browser app can all call the same agent if they can send JSON over HTTP.

### Discover the agent

```csharp
var card = await http.GetFromJsonAsync<JsonElement>("/a2a/weather/v1/card");
Console.WriteLine($"Agent name:    {card.GetProperty("name")}");
Console.WriteLine($"Description:   {card.GetProperty("description")}");
Console.WriteLine($"Version:       {card.GetProperty("version")}");
```

- The **AgentCard** tells clients what agent they are calling
- Metadata such as `name`, `description`, and `version` helps clients route or display agents
- Fetching the card first is the A2A equivalent of discovering a service contract

### Send an A2A message

```csharp
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
```

- `kind` describes the envelope type
- `role` tells the remote agent who sent the message
- `parts` holds the actual content, such as text
- `messageId` can be null when you want the server to assign one
- `contextId` ties multiple requests to the same conversation

### Call another endpoint the same way

```csharp
var response3 = await SendA2AMessage(http, "/a2a/trip-planning/v1/message:stream",
    "Help me plan a 3-day trip to Amsterdam in October.", "trip-planning-demo-1");
```

- The trip-planning endpoint may be a workflow behind the scenes
- From the client perspective, it still looks like one A2A agent
- This is the key benefit: implementation details stay behind the protocol boundary

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Tweak an AgentCard's metadata

In Module 10, find the Weather Agent's A2A hosting configuration and change metadata such as its name, description, or version. Restart Module 10, rerun this module, and verify the discovered AgentCard output changed.

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
