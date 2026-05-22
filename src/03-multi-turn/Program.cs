using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are TripBot, a friendly travel planning assistant. Keep answers brief.",
        name: "TripBot");

// Create a session to maintain conversation history across turns.
// Without a session, each RunAsync call is stateless — the agent forgets everything.
AgentSession session = await agent.CreateSessionAsync();

// Turn 1 — establish trip context
Console.WriteLine("Turn 1:");
Console.WriteLine(await agent.RunAsync("I'm planning a 10-day trip to Japan in October. My home airport is Dallas.", session));

// Turn 2 — agent remembers the Japan trip context
Console.WriteLine("\nTurn 2:");
Console.WriteLine(await agent.RunAsync("What are the must-see places in Kyoto?", session));

// Turn 3 — agent still has full context
Console.WriteLine("\nTurn 3:");
Console.WriteLine(await agent.RunAsync("Based on my trip, what should I pack for October weather in Japan?", session));

// Without session — each call is completely independent
Console.WriteLine("\n--- Without session (stateless) ---");
Console.WriteLine(await agent.RunAsync("What should I pack for my trip?"));
// Will say: "I don't have any information about your trip."
