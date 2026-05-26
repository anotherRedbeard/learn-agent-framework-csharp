using System.ComponentModel;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

var client = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());

// Define tools for the specialized weather agent
[Description("Get the current weather for a location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";

[Description("Get a 3-day weather forecast for a location.")]
static string GetForecast([Description("The location")] string location)
    => $"3-day forecast for {location}: Day 1: Cloudy 15°C, Day 2: Sunny 18°C, Day 3: Rainy 12°C.";

// Create a specialized inner agent — it has its own instructions and tools
AIAgent weatherAgent = client.AsAIAgent(
    model: deploymentName,
    instructions: "You are a weather expert. Answer questions about weather accurately and concisely.",
    name: "WeatherAgent",
    description: "An agent that answers weather-related questions including current conditions and forecasts.",
    tools: [
        AIFunctionFactory.Create(GetWeather),
        AIFunctionFactory.Create(GetForecast)
    ]);

// The outer agent uses the inner agent as a tool via .AsAIFunction()
// The outer agent doesn't know (or care) about the inner agent's tools or instructions
// It just knows it can delegate weather-related questions to it
AIAgent orchestratorAgent = client.AsAIAgent(
    model: deploymentName,
    instructions: "You are TripBot, a travel planning assistant. " +
                  "For weather-related questions, delegate to the weather agent. " +
                  "Synthesize all results into a cohesive travel recommendation.",
    tools: [weatherAgent.AsAIFunction()]);

Console.WriteLine("--- TripBot delegates weather questions to the WeatherAgent ---");
var prompt = "I'm planning a trip to Amsterdam next week — is the weather good for sightseeing?";
Console.WriteLine($"> {prompt}");
Console.WriteLine(await orchestratorAgent.RunAsync(prompt));

Console.WriteLine();
var prompt2 = "Should I pack a raincoat for my Tokyo trip this week?";
Console.WriteLine($"> {prompt2}");
Console.WriteLine(await orchestratorAgent.RunAsync(prompt2));
