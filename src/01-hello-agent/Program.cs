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
        instructions: "You are a friendly assistant. Keep your answers brief.",
        name: "HelloAgent");

// Non-streaming: get the complete response at once
Console.WriteLine("--- Non-streaming ---");
Console.WriteLine(await agent.RunAsync("What is the largest city in France?"));

// Streaming: receive tokens as they are generated
Console.WriteLine("\n--- Streaming ---");
await foreach (var update in agent.RunStreamingAsync("Tell me a one-sentence fun fact about space."))
{
    Console.Write(update);
}
Console.WriteLine();
