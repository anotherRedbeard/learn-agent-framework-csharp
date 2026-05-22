# Best Practices

## ✅ DO: Use ManagedIdentityCredential in production

```csharp
// ✅ Production
new ManagedIdentityCredential()

// ⚠️ Development only — probes multiple credential sources, adds latency
new DefaultAzureCredential()
```

## ✅ DO: Store secrets in user-secrets or environment variables

```bash
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://..."
```

**Never** put secrets in `appsettings.json`, hardcoded in source, or committed to git.

## ✅ DO: Write clear tool descriptions

The model selects tools based on their names and descriptions. Vague descriptions → wrong tool selection.

```csharp
// ❌ Too vague
[Description("Does stuff with data.")]

// ✅ Specific and actionable
[Description("Queries the inventory database for product availability by SKU.")]
```

## ✅ DO: Handle tool errors gracefully

Return clear error messages from tools so the model can reason about failures:

```csharp
static string GetWeather(string location)
{
    try { return FetchWeather(location); }
    catch (Exception ex) { return $"Error fetching weather for {location}: {ex.Message}"; }
}
```

## ✅ DO: Use sessions for multi-turn conversations

Without a session, each `RunAsync` is stateless. Always create a session for conversational agents.

## ❌ DON'T: Register too many tools

Every tool definition consumes tokens. Register only the tools the agent actually needs. Prefer focused agents over one agent that does everything.

## ❌ DON'T: Make tools overly permissive

```csharp
// ❌ Security risk — runs any SQL
[Description("Runs any SQL query")]
static string RunSql(string sql) { ... }

// ✅ Scoped to a specific, well-defined operation
[Description("Gets order status by order ID")]
static string GetOrderStatus(string orderId) { ... }
```

## ❌ DON'T: Use workflows when a single agent will do

> "Before reaching for workflows, we recommend you first try simpler patterns. They are easier to set up and debug. Workflows are most useful when you need guaranteed execution order that a single agent can't reliably provide on its own." — [Microsoft Docs](https://learn.microsoft.com/en-us/agent-framework/journey/workflows)

## ❌ DON'T: Make Skills too broad

A skill called "everything-about-finance" covering accounting, taxes, expenses, and payroll is too unfocused. Keep skills scoped to one domain.

## ❌ DON'T: Skip tool approval for sensitive actions

If a tool can make irreversible changes (send emails, delete records, transfer money), add tool approval to keep a human in the loop.

## ✅ DO: Add both streaming and non-streaming middleware

```csharp
// ✅ Provide both — streaming falls back to non-streaming if only one is provided
agent.AsBuilder()
    .Use(runFunc: MyMiddleware, runStreamingFunc: MyStreamingMiddleware)
    .Build();
```

## ✅ DO: Use the simplest pattern that meets your requirements

```
Need an agent? → AIAgent with instructions
Need external data? → Add function tools  
Need reusable expertise? → Add skills
Need guardrails/logging? → Add middleware
Need persistent memory? → Use context providers
Need agent composition? → Agents as tools (same process)
Need cross-service agents? → A2A protocol
Need guaranteed execution order? → Workflows
Need to expose your agent? → Hosting + A2A
```
