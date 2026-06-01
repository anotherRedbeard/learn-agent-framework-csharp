using A2A;
using A2A.AspNetCore;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

string endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set. Use dotnet user-secrets or environment variables.");
string deploymentName = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

// Register the shared chat client in DI as a keyed singleton.
// The key ("chat-model") must match the chatClientServiceKey passed to AddAIAgent().
// Keyed singletons let multiple agents share one client, or you can register
// different clients under different keys for agents that need different models.
IChatClient chatClient = new AIProjectClient(
        new Uri(endpoint),
        new DefaultAzureCredential())
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(deploymentName);

builder.Services.AddKeyedSingleton("chat-model", chatClient);

// Register agents in DI using AddAIAgent().
// This is the correct hosted pattern — agents are resolved from DI,
// not created inline, so they can participate in the DI lifecycle.
var weatherAgent = builder.AddAIAgent(
    "weather",
    instructions: "You are a destination weather expert. When given a travel destination, provide current conditions and advice on whether the weather is suitable for travel.",
    description: "Provides weather conditions for travel destinations.",
    chatClientServiceKey: "chat-model")
    .AddA2AServer();

var travelAgent = builder.AddAIAgent(
    "travel",
    instructions: "You are TripBot, a travel itinerary planner. Given a destination and trip context, create helpful day-by-day itineraries, suggest activities, and provide practical travel tips.",
    description: "Creates travel itineraries and trip recommendations.",
    chatClientServiceKey: "chat-model")
    .AddA2AServer();

// Register a sequential workflow: weather → travel
// The user's request goes to the weather agent first, then the travel agent builds on it.
var planningWorkflow = builder.AddWorkflow("trip-planning", (sp, key) =>
{
    var weather = sp.GetRequiredKeyedService<AIAgent>("weather");
    var travel = sp.GetRequiredKeyedService<AIAgent>("travel");
    return AgentWorkflowBuilder.BuildSequential(key, [weather, travel]);
});

// Workflows don't natively support A2A — wrap as an AIAgent first
var planningWorkflowAsAgent = planningWorkflow.AddAsAIAgent()
    .AddA2AServer();

var app = builder.Build();

// Expose each agent via A2A (HTTP-JSON transport).
// We use MapHttpA2A directly (instead of the convenience MapA2AHttpJson) so we can
// supply a real AgentCard — the convenience overload hardcodes Name = "A2A Agent".
// AgentCards are the discovery document any A2A client (see Module 08) reads first.
MapAgentWithCard(app, "weather", "/a2a/weather",
    description: "Provides weather conditions for travel destinations.");
MapAgentWithCard(app, "travel", "/a2a/travel",
    description: "Creates travel itineraries and trip recommendations.");
MapAgentWithCard(app, "trip-planning", "/a2a/trip-planning",
    description: "Sequential workflow: gathers weather, then builds a trip itinerary.");

app.Run();

// Resolves the keyed A2AServer registered by .AddA2AServer() and maps it with
// a populated AgentCard. The MS hosting library doesn't expose a card hook yet,
// so we drop down to the A2A.AspNetCore primitive.
static void MapAgentWithCard(WebApplication app, string agentName, string path, string description)
{
    var server = app.Services.GetRequiredKeyedService<A2AServer>(agentName);
    var card = new AgentCard
    {
        Name = agentName,
        Description = description,
        Version = "1.0.0",
    };
    app.MapHttpA2A(server, card, path);
}
