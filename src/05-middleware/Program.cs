using System.ComponentModel;
using System.Runtime.CompilerServices;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

[Description("Get the current weather for a location.")]
static string GetWeather([Description("The location")] string location)
    => $"Sunny in {location}, 22°C.";

AIAgent baseAgent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are TripBot, a travel planning assistant.",
        tools: [AIFunctionFactory.Create(GetWeather)]);

// Chain middleware using the builder pattern.
// Middleware wraps the agent in layers — first registered is outermost.
// Both streaming and non-streaming variants should be provided.
AIAgent agentWithMiddleware = baseAgent
    .AsBuilder()
        .Use(runFunc: LoggingMiddleware, runStreamingFunc: LoggingStreamingMiddleware)
        .Use(CustomFunctionCallingMiddleware)
    .Build();

Console.WriteLine("--- Agent with middleware ---");
Console.WriteLine(await agentWithMiddleware.RunAsync("I'm planning a trip to Paris. What's the weather like there?"));

// --- Agent Run Middleware ---
// Wraps the full agent run. Can inspect/modify input and output.
async Task<AgentResponse> LoggingMiddleware(
    IEnumerable<ChatMessage> messages,
    AgentSession? session,
    AgentRunOptions? options,
    AIAgent innerAgent,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"  [Middleware] → Request with {messages.Count()} message(s)");
    var response = await innerAgent.RunAsync(messages, session, options, cancellationToken);
    Console.WriteLine($"  [Middleware] ← Response with {response.Messages.Count} message(s)");
    return response;
}

// --- Streaming Middleware ---
// Always provide both streaming and non-streaming variants.
async IAsyncEnumerable<AgentResponseUpdate> LoggingStreamingMiddleware(
    IEnumerable<ChatMessage> messages,
    AgentSession? session,
    AgentRunOptions? options,
    AIAgent innerAgent,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    Console.WriteLine($"  [Middleware] → Streaming request with {messages.Count()} message(s)");
    var updates = new List<AgentResponseUpdate>();
    await foreach (var update in innerAgent.RunStreamingAsync(messages, session, options, cancellationToken))
    {
        updates.Add(update);
        yield return update;
    }
    Console.WriteLine($"  [Middleware] ← Streaming complete, {updates.Count} update(s)");
}

// --- Function Calling Middleware ---
// Intercepts individual tool invocations. Can validate arguments, log calls, or short-circuit.
async ValueTask<object?> CustomFunctionCallingMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"  [Tool Middleware] Calling tool: {context.Function.Name}");
    var result = await next(context, cancellationToken);
    Console.WriteLine($"  [Tool Middleware] Tool result: {result}");
    return result;
}
