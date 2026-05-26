// ============================================================
// Module 11 Scaffold — Persistent Agents
// ============================================================
// Use this file to build Module 11 from scratch.
// Copy it over Program.cs, then fill in every TODO.
// Run `dotnet run` after each TODO to see what changed.
//
//   cp Program.scaffold.cs Program.cs
//   dotnet run
// ============================================================

// TODO: Add using statements for:
//   - Azure.AI.Agents.Persistent
//   - Azure.Identity



// TODO: Read AZURE_OPENAI_ENDPOINT from environment variables.
//       Throw a descriptive InvalidOperationException if it's missing.
var endpoint = "";

// TODO: Read AZURE_OPENAI_DEPLOYMENT_NAME from environment variables.
//       Fall back to "gpt-4o-mini" if it's not set.
var deploymentName = "";

// TODO: Create a PersistentAgentsClient.
//       - Connect to the endpoint
//       - Authenticate with DefaultAzureCredential
var client = null!;

// TODO: Create a persistent agent as a server-side Foundry resource.
//       - Use client.Administration.CreateAgentAsync()
//       - Use deploymentName as the model
//       - Name it "TripBot-Persistent"
//       - Give it brief travel-planning instructions
PersistentAgent agent = null!;
Console.WriteLine($"Created agent: {agent.Id} ({agent.Name})");

// TODO: Create a thread with client.Threads.CreateThreadAsync().
//       This is the server-side conversation history.
PersistentAgentThread thread = null!;
Console.WriteLine($"Created thread: {thread.Id}");

// TODO: Create a user message on the thread with client.Messages.CreateMessageAsync().
//       Ask TripBot for the top 3 things to do in Paris.


// TODO: Create a run with client.Runs.CreateRunAsync(thread.Id, agent.Id).
//       Then poll with client.Runs.GetRunAsync() while the status is Queued or InProgress.
//       Include a short Task.Delay between polls.
ThreadRun run = null!;

Console.WriteLine($"Run completed with status: {run.Status}");

// TODO: If the run did not complete, print run.LastError?.Message and return.


// TODO: Retrieve messages with client.Messages.GetMessagesAsync().
//       Use ListSortOrder.Ascending so the prompt appears before the reply.
//       For each MessageTextContent item, print [$"{message.Role}"] and the text.
Console.WriteLine("\n--- Conversation ---");

Console.WriteLine($"\nAgent {agent.Id} is now visible in ai.azure.com under your project's Agents tab.");
Console.WriteLine("Set DELETE_AGENT=true to delete it on next run, or delete from the portal.");

// TODO: If DELETE_AGENT is set to true, delete the thread and agent.
//       Use client.Threads.DeleteThreadAsync(thread.Id) and
//       client.Administration.DeleteAgentAsync(agent.Id).
