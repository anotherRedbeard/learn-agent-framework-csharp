using System.ComponentModel;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// Define tools as regular methods with [Description] attributes.
// The framework uses these descriptions to help the model decide when to call the tool.
[Description("Get the current weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";

[Description("Get the current time in a given city.")]
static string GetTime([Description("The city to get the time for.")] string city)
    => $"The current time in {city} is 14:32 local time.";

// Register the tools with the agent using AIFunctionFactory.Create.
// The framework automatically handles the tool-calling loop:
// 1. Model decides to call a tool
// 2. Framework executes the function
// 3. Result is sent back to the model
// 4. Model continues reasoning
AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are TripBot, a travel planning assistant. Use tools to help users with weather and timing when planning trips.",
        tools: [
            AIFunctionFactory.Create(GetWeather),
            AIFunctionFactory.Create(GetTime)
        ]);

// The agent will automatically call GetWeather when relevant
Console.WriteLine(await agent.RunAsync("I'm heading to Amsterdam next week. What's the weather like there?"));

Console.WriteLine();

// The agent may call multiple tools in a single turn
Console.WriteLine(await agent.RunAsync("I'm flying to Tokyo tomorrow. What's the weather and local time there right now?"));
