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
    instructions: "You are a weather expert. Answer questions about weather accurately and concisely.",
    description: "An agent that answers weather-related questions.",
    chatClientServiceKey: "chat-model");

var travelAgent = builder.AddAIAgent(
    "travel",
    instructions: "You are a travel planning assistant. Help users plan trips, suggest destinations, and provide travel tips.",
    description: "An agent that helps with travel planning.",
    chatClientServiceKey: "chat-model");

// Register a sequential workflow: weather → travel
// The user's request goes to the weather agent first, then the travel agent builds on it.
var planningWorkflow = builder.AddWorkflow("trip-planning", (sp, key) =>
{
    var weather = sp.GetRequiredKeyedService<AIAgent>("weather");
    var travel = sp.GetRequiredKeyedService<AIAgent>("travel");
    return AgentWorkflowBuilder.BuildSequential(key, [weather, travel]);
});

// Workflows don't natively support A2A — wrap as an AIAgent first
var planningWorkflowAsAgent = planningWorkflow.AddAsAIAgent();

var app = builder.Build();

// Expose each agent via A2A (HTTP-JSON transport).
// contextId in the request maintains conversation history across calls.
// Use requests.http to test these endpoints once the app is running.
app.MapA2AHttpJson(weatherAgent, "/a2a/weather");
app.MapA2AHttpJson(travelAgent, "/a2a/travel");
app.MapA2AHttpJson(planningWorkflowAsAgent, "/a2a/trip-planning");

app.Run();
