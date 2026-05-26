// ============================================================
// Module 02 Scaffold — Add Tools
// ============================================================
// Use this file to build Module 02 from scratch.
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

// TODO: Define a GetWeather tool.
//       - Add a [Description] attribute to the method
//       - Add a [Description] attribute to the location parameter
//       - Return a simple weather string for the requested location
static string GetWeather(string location)
    => throw new NotImplementedException();

// TODO: Define a GetTime tool.
//       - Add a [Description] attribute to the method
//       - Add a [Description] attribute to the city parameter
//       - Return a simple local time string for the requested city
static string GetTime(string city)
    => throw new NotImplementedException();

// TODO: Create an AIAgent using AIProjectClient.
//       - Connect to the endpoint with DefaultAzureCredential
//       - Call .AsAIAgent() with a model and instructions
//       - Register GetWeather and GetTime with AIFunctionFactory.Create
AIAgent agent = null!;

// TODO: Use agent.RunAsync() to ask a question that should call GetWeather.
//       Print the response with Console.WriteLine().


Console.WriteLine();

// TODO: Use agent.RunAsync() to ask a question that may call both tools.
//       Print the response with Console.WriteLine().
