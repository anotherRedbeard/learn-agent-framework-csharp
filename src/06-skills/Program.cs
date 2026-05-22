using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

// NOTE: AgentSkillsProvider lives in Microsoft.Agents.AI (the base package) —
// there is no separate Microsoft.Agents.AI.Skills package.

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// AgentSkillsProvider is a context provider that makes skills available to agents.
// Skills are discovered from the 'skills' directory — each subdirectory with a SKILL.md is a skill.
// Progressive disclosure: only names/descriptions (~100 tokens) load upfront;
// full instructions load on demand when the agent actually needs them.
var skillsProvider = new AgentSkillsProvider(
    Path.Combine(AppContext.BaseDirectory, "skills"));

AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "SkillsAgent",
        ChatOptions = new() { Instructions = "You are a helpful HR assistant." },
        AIContextProviders = [skillsProvider],
    });

AgentSession session = await agent.CreateSessionAsync();

// The agent will automatically load and use the expense-report skill
Console.WriteLine(await agent.RunAsync("I need to file an expense report for a $450 flight to Paris.", session));
Console.WriteLine();
Console.WriteLine(await agent.RunAsync("What is the spending limit per trip?", session));
