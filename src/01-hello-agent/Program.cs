using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// Create the agent — just a model client + instructions
AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are TripBot, a friendly travel planning assistant. Help users explore destinations, plan itineraries, and answer travel questions. Keep answers brief.",
        name: "TripBot");

// Non-streaming: get the complete response at once
Console.WriteLine("--- Non-streaming ---");
Console.WriteLine(await agent.RunAsync("What are the top 3 things to do in Paris?"));

// Streaming: receive tokens as they are generated
Console.WriteLine("\n--- Streaming ---");
await foreach (var update in agent.RunStreamingAsync("Give me a one-sentence travel tip for first-time visitors to Tokyo."))
{
    Console.Write(update);
}
Console.WriteLine();
