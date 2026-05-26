using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// --- Example 1: Default in-memory history ---
// When using Chat Completion (not Foundry Agent Service), the framework
// automatically creates an InMemoryChatHistoryProvider for you.
Console.WriteLine("=== Example 1: Default In-Memory History ===");

AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are TripBot, a travel planning assistant. Keep answers brief.",
        name: "TripBot");

AgentSession session = await agent.CreateSessionAsync();

var prompt = "I prefer window seats and always travel carry-on only.";
Console.WriteLine($"> {prompt}");
Console.WriteLine(await agent.RunAsync(prompt, session));

var prompt2 = "My home airport is Dallas Fort Worth.";
Console.WriteLine($"> {prompt2}");
Console.WriteLine(await agent.RunAsync(prompt2, session));

var prompt3 = "What do you know about my travel preferences so far?";
Console.WriteLine($"> {prompt3}");
Console.WriteLine(await agent.RunAsync(prompt3, session));  // Remembers preferences

// --- Example 2: Custom ChatHistoryProvider ---
// You can provide your own implementation to store history in a database,
// Redis cache, or any other persistent store.
Console.WriteLine("\n=== Example 2: Custom ChatHistoryProvider ===");

AIAgent agentWithCustomHistory = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new() { ModelId = deploymentName, Instructions = "You are TripBot, a travel planning assistant." },
        ChatHistoryProvider = new LoggingChatHistoryProvider(),
        // Foundry's Responses API tracks conversation state server-side via previous_response_id.
        // Setting these flags lets us use a custom ChatHistoryProvider anyway — the framework will
        // silently clear the server-side conversation id and use our provider's history instead.
        ThrowOnChatHistoryProviderConflict = false,
        WarnOnChatHistoryProviderConflict = false
    });

AgentSession session2 = await agentWithCustomHistory.CreateSessionAsync();
var prompt4 = "I need vegetarian meal options on my flights.";
Console.WriteLine($"> {prompt4}");
Console.WriteLine(await agentWithCustomHistory.RunAsync(prompt4, session2));

var prompt5 = "What dietary preference did I mention?";
Console.WriteLine($"> {prompt5}");
Console.WriteLine(await agentWithCustomHistory.RunAsync(prompt5, session2));

// --- Example 3: AIContextProvider (RAG / per-turn context injection) ---
// AIContextProviders are different from ChatHistoryProvider:
//
//   ChatHistoryProvider = WHERE the transcript is stored (replaces default storage)
//   AIContextProvider   = WHAT extra info to add before each LLM call (RAG, preferences, dynamic tools)
//
// You can use both at once. This example shows a provider that, on every turn,
// pulls the user's profile from a "database" (simulated here as a static record)
// and injects it as a system message — exactly how you'd wire up a RAG lookup,
// a memory store like Mem0, or a personalization service.
Console.WriteLine("\n=== Example 3: AIContextProvider (per-turn injection) ===");

AIAgent agentWithContext = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new() { ModelId = deploymentName, Instructions = "You are TripBot, a travel planning assistant. Keep answers brief." },
        AIContextProviders = [new UserPreferencesContextProvider()]
    });

AgentSession session3 = await agentWithContext.CreateSessionAsync();
var prompt6 = "Suggest a weekend trip for me.";
Console.WriteLine($"> {prompt6}");
Console.WriteLine(await agentWithContext.RunAsync(prompt6, session3));
// Notice: the agent uses the injected preferences even though the user never typed them this session.

// --- Custom ChatHistoryProvider implementation ---
// Subclass ChatHistoryProvider and override two methods:
//   ProvideChatHistoryAsync — called BEFORE each run; return stored messages to inject as history
//   StoreChatHistoryAsync  — called AFTER each run; persist the new request+response messages
//
// The framework merges your returned history with the current user message automatically.
class LoggingChatHistoryProvider : ChatHistoryProvider
{
    // In a real implementation, key this by session ID: Dictionary<string, List<ChatMessage>>
    private readonly List<ChatMessage> _history = [];

    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"  [History] Providing {_history.Count} stored messages");
        return ValueTask.FromResult<IEnumerable<ChatMessage>>(_history);
    }

    protected override ValueTask StoreChatHistoryAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        // context.RequestMessages  — what was sent to the model this turn
        // context.ResponseMessages — what the model replied with this turn
        if (context.RequestMessages is { } request) _history.AddRange(request);
        if (context.ResponseMessages is { } response) _history.AddRange(response);
        Console.WriteLine($"  [History] Stored {context.RequestMessages?.Count() ?? 0} request + {context.ResponseMessages?.Count() ?? 0} response messages");
        return ValueTask.CompletedTask;
    }
}

// --- Custom AIContextProvider implementation ---
// Subclass MessageAIContextProvider and override one method:
//   ProvideMessagesAsync — called BEFORE each run; return additional messages to inject
//
// This is where you put RAG lookups, user profile fetches, memory recall, dynamic tools, etc.
// In a real app you'd hit a vector DB / profile store / cache here instead of using a static record.
#pragma warning disable MAAI001 // InvokingContext is evaluation-only API
class UserPreferencesContextProvider : MessageAIContextProvider
{
    protected override ValueTask<IEnumerable<ChatMessage>> ProvideMessagesAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        // Pretend we just queried a profile DB / RAG index for the current user.
        var profile = """
            User profile (loaded fresh each turn from an external store):
            - Prefers window seats
            - Vegetarian
            - Home airport: Dallas Fort Worth (DFW)
            - Budget-conscious traveler
            """;

        Console.WriteLine("  [Context] Injecting user profile for this turn");
        return ValueTask.FromResult<IEnumerable<ChatMessage>>(
            [new ChatMessage(ChatRole.System, profile)]);
    }
}
#pragma warning restore MAAI001
