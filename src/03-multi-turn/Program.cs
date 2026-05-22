using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are a friendly assistant. Keep your answers brief.",
        name: "ConversationAgent");

// Create a session to maintain conversation history across turns.
// Without a session, each RunAsync call is stateless — the agent forgets everything.
AgentSession session = await agent.CreateSessionAsync();

// Turn 1
Console.WriteLine("Turn 1:");
Console.WriteLine(await agent.RunAsync("My name is Alice and I love hiking.", session));

// Turn 2 — the agent remembers the user's name and hobby
Console.WriteLine("\nTurn 2:");
Console.WriteLine(await agent.RunAsync("What do you remember about me?", session));

// Turn 3
Console.WriteLine("\nTurn 3:");
Console.WriteLine(await agent.RunAsync("Can you suggest a hiking trail based on what you know about me?", session));

// Without session — each call is completely independent
Console.WriteLine("\n--- Without session (stateless) ---");
Console.WriteLine(await agent.RunAsync("What do you remember about me?"));
// Will say: "I don't have any information about you."
