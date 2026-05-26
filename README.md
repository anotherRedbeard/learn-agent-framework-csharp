# learn-agent-framework-csharp

> **Go from zero to hero with the [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/) in C#.**

This repo teaches you how to build AI agents the *right* way — progressively, with working code and clear explanations of *why* each pattern exists. Each module introduces exactly one new concept, so you're never overwhelmed.

## What is the Microsoft Agent Framework?

The Microsoft Agent Framework is a library for building production-grade AI agents in C# (and Python). It wraps large language models (LLMs) with the structure real applications need:

- **Tools** — let the agent call your code (APIs, databases, functions)
- **Sessions** — maintain conversation history across turns
- **Middleware** — add logging, guardrails, and telemetry without touching agent logic
- **Skills** — package and reuse domain expertise across agents
- **Workflows** — orchestrate multi-agent, multi-step processes with guaranteed order
- **Hosting** — expose agents over the A2A protocol so any service can call them

The framework abstracts away which LLM provider you use — swap from Azure OpenAI to any other provider without changing your agent code.

## Prerequisites & Getting Started

1. Follow [docs/prerequisites.md](docs/prerequisites.md) for full setup instructions
2. Deploy Azure resources: `cd infra && ./deploy.sh` (or `.\deploy.ps1` on Windows)
3. Once your credentials are configured → **[Start here: src/README.md](src/README.md)**

**Quick checklist:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Azure CLI logged in: `az login`
- Azure resources deployed (run `infra/deploy.sh` or see [docs/prerequisites.md](docs/prerequisites.md))
- Credentials set via `dotnet user-secrets`

## Modules

Follow these in order if you're new to Agent Framework. Jump to any module if you already know the basics.

| Module | Concept | Key APIs |
|--------|---------|----------|
| [01 — Hello Agent](src/01-hello-agent/) | Create your first agent, invoke it, stream responses | `AIAgent`, `RunAsync`, `RunStreamingAsync` |
| [02 — Add Tools](src/02-add-tools/) | Give agents function tools to act on the world | `AIFunctionFactory.Create`, `[Description]` |
| [03 — Multi-Turn](src/03-multi-turn/) | Maintain conversation state across turns | `AgentSession`, `CreateSessionAsync` |
| [04 — Memory & Context Providers](src/04-memory-context-providers/) | Inject persistent memory and dynamic context | `ChatHistoryProvider`, `AgentSession` |
| [05 — Middleware](src/05-middleware/) | Intercept and customize agent behavior | `AsBuilder().Use(...)`, middleware pipeline |
| [06 — Skills](src/06-skills/) | Package reusable domain expertise | `AgentSkillsProvider`, `SKILL.md` |
| [07 — Agents as Tools](src/07-agents-as-tools/) | Compose agents by using one as a tool for another | `AsAIFunction()` |
| [08 — Agent-to-Agent (A2A)](src/08-agent-to-agent/) | Inter-agent communication across service boundaries | `MapA2A`, A2A protocol |
| [09 — Workflows](src/09-workflows/) | Explicit multi-step orchestration | `WorkflowBuilder`, `Executor<T>` |
| [10 — Hosting](src/10-hosting/) | Expose agents via ASP.NET Core + A2A | `AddAIAgent`, `AddWorkflow`, DI hosting |

## Core Concepts

See [docs/concepts.md](docs/concepts.md) for an overview of the framework's core abstractions.

## Best Practices

See [docs/best-practices.md](docs/best-practices.md) for patterns to follow and anti-patterns to avoid.

## The Journey

This repo follows the [Microsoft Agent Framework journey](https://learn.microsoft.com/en-us/agent-framework/journey/):

```
LLM Fundamentals → First Agent → Tools → Skills → Middleware → Context Providers → Agents as Tools → A2A → Workflows → Hosting
```

Each step adds capability. Use the simplest pattern that meets your requirements.

## Running a module

```bash
cd src/01-hello-agent
dotnet run
```

Make sure your environment variables are set first (see [.env.example](.env.example)).

## References

- [Microsoft Agent Framework docs](https://learn.microsoft.com/en-us/agent-framework/)
- [Get Started tutorial](https://learn.microsoft.com/en-us/agent-framework/get-started/)
- [The Journey guide](https://learn.microsoft.com/en-us/agent-framework/journey/)
- [GitHub samples](https://github.com/microsoft/agent-framework)
