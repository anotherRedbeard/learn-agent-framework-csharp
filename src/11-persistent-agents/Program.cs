using Azure.AI.Agents.Persistent;
using Azure.Identity;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// PersistentAgentsClient talks to the Azure AI Agents service. Unlike
// AIProjectClient.AsAIAgent (which builds a client-side agent), the agents
// created here are real resources stored inside your Foundry project — visible
// at https://ai.azure.com under Project → Agents.
var client = new PersistentAgentsClient(endpoint, new DefaultAzureCredential());

// 1. Create the agent (server-side resource).
PersistentAgent agent = await client.Administration.CreateAgentAsync(
    model: deploymentName,
    name: "TripBot-Persistent",
    instructions: "You are TripBot, a friendly travel planning assistant. Keep answers brief.");

Console.WriteLine($"Created agent: {agent.Id} ({agent.Name})");

// 2. Create a thread — the server-side equivalent of an AgentSession.
PersistentAgentThread thread = await client.Threads.CreateThreadAsync();
Console.WriteLine($"Created thread: {thread.Id}");

// 3. Post a user message to the thread.
var prompt = "What are the top 3 things to do in Paris?";
Console.WriteLine($"> {prompt}");
await client.Messages.CreateMessageAsync(
    threadId: thread.Id,
    role: MessageRole.User,
    content: prompt);

// 4. Kick off a run and poll until it reaches a terminal state.
ThreadRun run = await client.Runs.CreateRunAsync(thread.Id, agent.Id);
while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
{
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    run = await client.Runs.GetRunAsync(thread.Id, run.Id);
}

Console.WriteLine($"Run completed with status: {run.Status}");

if (run.Status != RunStatus.Completed)
{
    Console.WriteLine($"Run failed: {run.LastError?.Message}");
    return;
}

// 5. Read messages back — ascending so the user prompt comes before the reply.
Console.WriteLine("\n--- Conversation ---");
await foreach (PersistentThreadMessage message in client.Messages.GetMessagesAsync(thread.Id, order: ListSortOrder.Ascending))
{
    foreach (var item in message.ContentItems)
    {
        if (item is MessageTextContent text)
        {
            Console.WriteLine($"[{message.Role}] {text.Text}");
        }
    }
}

Console.WriteLine($"\nAgent {agent.Id} is now visible in ai.azure.com under your project's Agents tab.");
Console.WriteLine("Set DELETE_AGENT=true to delete it on next run, or delete from the portal.");

if (string.Equals(Environment.GetEnvironmentVariable("DELETE_AGENT"), "true", StringComparison.OrdinalIgnoreCase))
{
    await client.Threads.DeleteThreadAsync(thread.Id);
    await client.Administration.DeleteAgentAsync(agent.Id);
    Console.WriteLine("Deleted thread and agent.");
}
