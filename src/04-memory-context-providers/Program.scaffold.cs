// ============================================================
// Module 04 Scaffold — Memory & Context Providers
// ============================================================
// Use this file to build Module 04 from scratch.
// Copy it over Program.cs, then fill in every TODO.
// Run `dotnet run` after each TODO to see what changed.
//
//   cp Program.scaffold.cs Program.cs
//   dotnet run
// ============================================================

// TODO: Add using statements for:
//   - Azure.AI.Projects
//   - Azure.Identity
//   - Microsoft.Agents.AI
//   - Microsoft.Extensions.AI



// TODO: Read AZURE_OPENAI_ENDPOINT from environment variables.
//       Throw a descriptive InvalidOperationException if it's missing.
var endpoint = "";

// TODO: Read AZURE_OPENAI_DEPLOYMENT_NAME from environment variables.
//       Fall back to "gpt-4o-mini" if it's not set.
var deploymentName = "";

// TODO: Print the label "=== Example 1: Default In-Memory History ===".

// TODO: Create an AIAgent using AIProjectClient.
//       - Connect to the endpoint with DefaultAzureCredential
//       - Call .AsAIAgent() with a model, instructions, and name
//       - Instructions should describe TripBot as a brief travel planning assistant
AIAgent agent = null!;

// TODO: Create an AgentSession from the agent.
//       This session is what lets later calls see earlier messages.
AgentSession session = null!;

// TODO: Call agent.RunAsync() three times with the same session.
//       - First, tell TripBot one travel preference
//       - Second, tell TripBot another travel fact
//       - Third, ask what it remembers about your preferences



// TODO: Print a blank line and the label "=== Example 2: Custom ChatHistoryProvider ===".

// TODO: Create a second AIAgent using ChatClientAgentOptions.
//       - Set ChatOptions.Instructions for TripBot
//       - Set ChatHistoryProvider to a new LoggingChatHistoryProvider
AIAgent agentWithCustomHistory = null!;

// TODO: Create a second AgentSession.
//       Then call RunAsync() twice with that session:
//       - First, tell TripBot a dietary preference
//       - Second, ask TripBot what dietary preference you mentioned
AgentSession session2 = null!;



// TODO: Implement LoggingChatHistoryProvider by subclassing ChatHistoryProvider.
//       It should store messages in a List<ChatMessage> for now.
//       In production, key history by session ID and persist it outside the process.
class LoggingChatHistoryProvider : ChatHistoryProvider
{
    // TODO: Add a private List<ChatMessage> field to hold history.

    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Print how many stored messages are being provided.
        // TODO: Return the stored messages as IEnumerable<ChatMessage>.
        return ValueTask.FromResult<IEnumerable<ChatMessage>>([]);
    }

    protected override ValueTask StoreChatHistoryAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Add context.RequestMessages and context.ResponseMessages to history.
        // TODO: Print how many request and response messages were stored.
        return ValueTask.CompletedTask;
    }
}
