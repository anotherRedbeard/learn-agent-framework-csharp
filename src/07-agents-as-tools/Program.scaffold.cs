// ============================================================
// Module 07 Scaffold — Agents as Tools
// ============================================================
// Use this file to build Module 07 from scratch.
// Copy it over Program.cs, then fill in every TODO.
// Run `dotnet run` after each TODO to see what changed.
//
//   cp Program.scaffold.cs Program.cs
//   dotnet run
// ============================================================

// TODO: Add using statements for:
//   - System.ComponentModel
//   - Azure.AI.Projects
//   - Azure.Identity
//   - Microsoft.Agents.AI
//   - Microsoft.Extensions.AI



// TODO: Read AZURE_OPENAI_ENDPOINT from environment variables.
//       Throw a descriptive InvalidOperationException if it's missing.
var endpoint = "";

// TODO: Read AZURE_OPENAI_DEPLOYMENT_NAME from environment variables.
//       Fall back to "gpt-4o-mini" if it's not set.
var deploymentName = "";

// TODO: Create one AIProjectClient using the endpoint and DefaultAzureCredential.
var client = null!;

// TODO: Create a GetWeather tool method.
//       - Add a Description attribute to the method
//       - Add a Description attribute to the location parameter
//       - Return a short fake current-weather response



// TODO: Create a GetForecast tool method.
//       - Add a Description attribute to the method
//       - Add a Description attribute to the location parameter
//       - Return a short fake 3-day forecast



// TODO: Create a WeatherAgent specialist with client.AsAIAgent().
//       - Give it focused weather instructions
//       - Give it a clear name and description
//       - Add GetWeather and GetForecast as tools with AIFunctionFactory.Create(...)
AIAgent weatherAgent = null!;

// TODO: Create a TripBot orchestrator agent with client.AsAIAgent().
//       - Tell it to delegate weather-related questions to the weather agent
//       - Add weatherAgent.AsAIFunction() as its only tool
AIAgent orchestratorAgent = null!;

// TODO: Ask TripBot a travel question that requires weather advice.
//       Print the label below before the response.
Console.WriteLine("--- TripBot delegates weather questions to the WeatherAgent ---");

// TODO: Ask TripBot a second travel question that should also use WeatherAgent.
