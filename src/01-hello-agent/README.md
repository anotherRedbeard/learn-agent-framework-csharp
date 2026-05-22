# Module 01 — Hello Agent

**Concept:** Create your first agent and get a response — in just a few lines of code.

## What you'll learn

- How `AIAgent` wraps an LLM with a persistent identity and instructions
- The difference between non-streaming (`RunAsync`) and streaming (`RunStreamingAsync`)
- Why you use an agent instead of calling the LLM API directly

## When to use this pattern

A basic agent with instructions only is the right starting point when:
- You need a simple Q&A or text-generation assistant
- The LLM's training knowledge is sufficient (no real-time data needed)
- You don't yet need tools, memory, or session management

## Key concepts

### What a raw LLM call gives you
A stateless request-response. No memory, no tools, no identity. You must manage system prompts, history, and tool orchestration yourself.

### What `AIAgent` adds
- **Instructions** — set once, applied to every call
- **Identity** — a named agent you can reuse and extend
- **Provider abstraction** — swap from Azure OpenAI to any other provider without changing your agent code

### Streaming vs. non-streaming
- Use `RunAsync` when you need the complete response before processing it
- Use `RunStreamingAsync` when you want to display tokens to the user as they arrive (better UX for long responses)

## Running this module

```bash
cd src/01-hello-agent
dotnet run
```

## Anti-patterns to avoid

❌ **Hardcoding the endpoint or API key** — use environment variables or `dotnet user-secrets`

❌ **Using `DefaultAzureCredential` in production** — it probes multiple credential sources and adds latency. Use `ManagedIdentityCredential` in production.

## References

- [Get Started: Your First Agent](https://learn.microsoft.com/en-us/agent-framework/get-started/your-first-agent)
- [Journey: From LLMs to Agents](https://learn.microsoft.com/en-us/agent-framework/journey/from-llms-to-agents)

---

**→ Next: [Module 02 — Add Tools](../02-add-tools/)**
