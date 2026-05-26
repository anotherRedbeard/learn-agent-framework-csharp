// ============================================================
// Module 10 Scaffold — Hosting
// ============================================================
// Use this file to build Module 10 from scratch.
// Copy it over Program.cs, then fill in every TODO.
// Run `dotnet run` after each TODO to see what changed.
//
//   cp Program.scaffold.cs Program.cs
//   dotnet run
// ============================================================

// TODO: Add using statements for:
//   - Azure.AI.Projects
//   - Azure.Identity
//   - Microsoft.Agents.AI
//   - Microsoft.Agents.AI.Hosting
//   - Microsoft.Agents.AI.Workflows
//   - Microsoft.Extensions.AI



// TODO: Create the WebApplication builder.
var builder = null!;

// TODO: Read AZURE_OPENAI_ENDPOINT from builder.Configuration.
//       Throw a descriptive InvalidOperationException if it's missing.
var endpoint = "";

// TODO: Read AZURE_OPENAI_DEPLOYMENT_NAME from builder.Configuration.
//       Throw a descriptive InvalidOperationException if it's missing.
var deploymentName = "";

// TODO: Create an IChatClient using AIProjectClient.
//       - Connect to the endpoint with DefaultAzureCredential
//       - Get the project OpenAI client
//       - Get the project Responses client
//       - Call .AsIChatClient(deploymentName)
IChatClient chatClient = null!;

// TODO: Register the chat client as a keyed singleton named "chat-model".


// TODO: Register a weather agent with AddAIAgent().
//       - Key: "weather"
//       - Instructions: answer destination weather questions for travelers
//       - Description: explain that it provides weather conditions for travel destinations
//       - chatClientServiceKey: "chat-model"
var weatherAgent = null!;

// TODO: Register a travel agent with AddAIAgent().
//       - Key: "travel"
//       - Instructions: create day-by-day itineraries and practical travel tips
//       - Description: explain that it creates travel itineraries and recommendations
//       - chatClientServiceKey: "chat-model"
var travelAgent = null!;

// TODO: Register a sequential workflow named "trip-planning".
//       Resolve the weather and travel agents from DI by key, then build a sequential workflow.
var planningWorkflow = null!;

// TODO: Convert the workflow to an AIAgent so it can be hosted over A2A.
var planningWorkflowAsAgent = null!;

// TODO: Build the app.
var app = null!;

// TODO: Map each agent to an A2A HTTP-JSON endpoint.
//       - weather -> /a2a/weather
//       - travel -> /a2a/travel
//       - trip-planning workflow agent -> /a2a/trip-planning


// TODO: Run the app.
