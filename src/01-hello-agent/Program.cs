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
var prompt = "What are the top 3 things to do in Paris?";
Console.WriteLine($"> {prompt}");
Console.WriteLine(await agent.RunAsync(prompt));

// Streaming: receive tokens as they are generated
Console.WriteLine("\n--- Streaming ---");
var prompt2 = "Give me a one-sentence travel tip for first-time visitors to Tokyo.";
Console.WriteLine($"> {prompt2}");
await foreach (var update in agent.RunStreamingAsync(prompt2))
{
    Console.Write(update);
}
Console.WriteLine();
