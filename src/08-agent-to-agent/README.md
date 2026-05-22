# Module 08 — Agent-to-Agent (A2A): Client Side

**Concept:** Call an agent exposed via the A2A protocol — from any language, any framework, over plain HTTP.

> ⚠️ **This module requires Module 10 to be running first.**
> ```bash
> # Terminal 1 — start the A2A server
> cd ../10-hosting
> dotnet run
>
> # Terminal 2 — run the A2A client (this module)
> cd ../08-agent-to-agent
> dotnet run
> ```

## What you'll learn

- How to discover an agent's capabilities via its **AgentCard**
- The A2A message format (`kind`, `role`, `parts`, `contextId`, `messageId`)
- How `contextId` maintains conversation history across separate HTTP requests
- Why A2A uses plain HTTP/JSON — any language can call it, no SDK needed

## What is A2A?

A2A (Agent-to-Agent) is an [open protocol](https://a2a-protocol.org/latest/) for agents to communicate across boundaries:

- **Service boundaries** — your agent and theirs run in different processes/services
- **Team boundaries** — you don't own the other agent's code
- **Organizational boundaries** — third-party agent providers
- **Language boundaries** — .NET agent calling a Python agent (or vice versa)

> "A2A is the HTTP of agent communication — framework-agnostic by design."

## When to use A2A vs. Agents as Tools (Module 07)

| Scenario | Use |
|----------|-----|
| Same process, same team | **Agents as Tools** — simpler, no network overhead |
| Different services/teams | **A2A** — cross-boundary, standard protocol |
| Third-party agent providers | **A2A** |
| Need language interoperability | **A2A** |

## The A2A message format

```json
{
  "message": {
    "kind": "message",
    "role": "user",
    "parts": [{ "kind": "text", "text": "What is the weather in Amsterdam?" }],
    "messageId": null,
    "contextId": "my-conversation-123"
  }
}
```

- **`contextId`** — reuse this across requests to maintain conversation history (the HTTP equivalent of `AgentSession`)
- **`messageId`** — can be null; the agent generates one
- **`parts`** — array of content (text, files, etc.)

## What this module demonstrates

1. **Agent discovery** — `GET /a2a/weather/v1/card` returns the AgentCard
2. **Single-turn call** — send a message, get a response
3. **Multi-turn via contextId** — follow-up question in the same conversation
4. **Calling a workflow** — the sequential workflow from Module 10 looks identical to a single agent from the client's perspective

## Why no Agent Framework packages?

This `.csproj` has zero Agent Framework dependencies. That's intentional — A2A is plain JSON over HTTP. Any HTTP client in any language can call an A2A-compliant agent. This demonstrates the true interoperability of the protocol.

## Running this module

```bash
# Terminal 1
cd src/10-hosting && dotnet run

# Terminal 2 (once the server is running)
cd src/08-agent-to-agent && dotnet run
```

## References

- [Journey: Agent-to-Agent](https://learn.microsoft.com/en-us/agent-framework/journey/agent-to-agent)
- [A2A Integration](https://learn.microsoft.com/en-us/agent-framework/integrations/a2a)
- [A2A Protocol Spec](https://a2a-protocol.org/latest/)

---

**→ Next: [Module 09 — Workflows](../09-workflows/)**
