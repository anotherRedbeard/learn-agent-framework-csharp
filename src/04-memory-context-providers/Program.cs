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

Console.WriteLine(await agent.RunAsync("I prefer window seats and always travel carry-on only.", session));
Console.WriteLine(await agent.RunAsync("My home airport is Dallas Fort Worth.", session));
Console.WriteLine(await agent.RunAsync("What do you know about my travel preferences so far?", session));  // Remembers preferences

// --- Example 2: Custom ChatHistoryProvider ---
// You can provide your own implementation to store history in a database,
// Redis cache, or any other persistent store.
Console.WriteLine("\n=== Example 2: Custom ChatHistoryProvider ===");

AIAgent agentWithCustomHistory = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new() { Instructions = "You are TripBot, a travel planning assistant." },
        ChatHistoryProvider = new LoggingChatHistoryProvider()
    });

AgentSession session2 = await agentWithCustomHistory.CreateSessionAsync();
Console.WriteLine(await agentWithCustomHistory.RunAsync("I need vegetarian meal options on my flights.", session2));
Console.WriteLine(await agentWithCustomHistory.RunAsync("What dietary preference did I mention?", session2));

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
        _history.AddRange(context.RequestMessages);
        _history.AddRange(context.ResponseMessages);
        Console.WriteLine($"  [History] Stored {context.RequestMessages.Count()} request + {context.ResponseMessages.Count()} response messages");
        return ValueTask.CompletedTask;
    }
}
