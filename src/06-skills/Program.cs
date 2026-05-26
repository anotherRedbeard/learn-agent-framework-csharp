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
// Also update the skills path comment
var skillsProvider = new AgentSkillsProvider(
    Path.Combine(AppContext.BaseDirectory, "skills"));

AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "TripBot",
        ChatOptions = new() { Instructions = "You are TripBot, a travel planning assistant that specializes in helping travelers understand entry requirements and travel policies." },
        AIContextProviders = [skillsProvider],
    });

AgentSession session = await agent.CreateSessionAsync();

// The agent will automatically load and use the visa-requirements skill
var prompt = "I'm a US citizen planning a trip to Japan. Do I need a visa?";
Console.WriteLine($"> {prompt}");
Console.WriteLine(await agent.RunAsync(prompt, session));
Console.WriteLine();

var prompt2 = "What about visiting France and Italy on the same trip?";
Console.WriteLine($"> {prompt2}");
Console.WriteLine(await agent.RunAsync(prompt2, session));
