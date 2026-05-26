# Module 06 — Skills

**Concept:** Package reusable domain expertise — instructions, reference material, and optional scripts — into self-contained units an agent can discover and use on demand.

## What you'll learn

- How `AgentSkillsProvider` advertises available skills to an agent
- How `SKILL.md` defines a skill's identity, description, and instructions
- Why skills are different from tools: skills package expertise, tools perform actions
- How progressive disclosure keeps detailed reference material out of the prompt until needed

## When to use this pattern

Skills are the right pattern when:
- You have reusable domain knowledge that belongs in one portable package
- Multiple agents need the same policy, workflow, or subject-matter guidance
- You want to include reference files or scripts without loading everything upfront
- A capability is more than one callable function — it needs instructions, examples, and supporting material

---

## Step 1 — Run it first

```bash
cd src/06-skills
dotnet run
```

You should see TripBot answer visa questions by using the `visa-requirements` skill from the local `skills/` directory. Once it's working, move on to Step 2.

---

## Step 2 — Code walkthrough

Open `Program.cs` and read through it alongside these explanations.

### Connect to the skill directory

```csharp
var skillsProvider = new AgentSkillsProvider(
    Path.Combine(AppContext.BaseDirectory, "skills"));
```

- `AgentSkillsProvider` scans a skills directory and exposes each folder with a `SKILL.md` file
- The folder name is the skill name — here, `skills/visa-requirements/`
- The agent initially sees only the skill metadata, not every reference file
- Full instructions and resources are loaded only when the agent decides the skill is relevant

> **Why not put all visa rules in the system prompt?** You could — but every request would pay that context cost. Skills let the agent discover domain expertise first, then load details only when needed.

### Attach skills to the agent

```csharp
AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "TripBot",
        ChatOptions = new() { ModelId = deploymentName, Instructions = "You are TripBot..." },
        AIContextProviders = [skillsProvider],
    });
```

- `AIContextProviders` adds dynamic context sources to the agent
- The skills provider gives TripBot access to packaged travel expertise
- `instructions` still define the agent's role; the skill adds specialized knowledge when needed

### Ask questions that trigger a skill

```csharp
AgentSession session = await agent.CreateSessionAsync();
var prompt = "I'm a US citizen planning a trip to Japan. Do I need a visa?";
Console.WriteLine($"> {prompt}");
Console.WriteLine(await agent.RunAsync(prompt, session));
```

- `AgentSession` keeps the conversation together across multiple calls
- The visa question matches the `visa-requirements` skill description
- The agent can load `SKILL.md` and supporting files to answer with more specific guidance

---

## Step 3 — Your turn 🛠️

Work through these challenges in order. Each one builds on the previous.

### 🟢 Starter — Edit an existing skill

Open `skills/visa-requirements/SKILL.md` and update the instructions so TripBot always asks for trip dates before giving final visa guidance. Run it and verify the answer changes.

### 🟡 Intermediate — Create a new skill

Add a new folder under `skills/` with its own `SKILL.md`. Give it clear frontmatter, focused instructions, and at least one supporting reference file or script. Run the module and ask a question that should trigger your new skill.

### 🔴 Stretch — Build a skill registry

Replace the hardcoded local skills path with a small registry that can load skills from another source, such as a checked-out shared folder, a downloaded package, or a remote catalog. Keep the same `AgentSkillsProvider` integration, but make the source configurable.

> **Hint:** You're packaging domain expertise, not just adding another tool. A good skill includes when to use it, how to reason in that domain, and where to find deeper reference material.

---

## Step 4 — Build it from scratch (optional)

Want to prove you understand it? Delete `Program.cs` contents and rebuild from `Program.scaffold.cs`:

```bash
# In src/06-skills/
cp Program.scaffold.cs Program.cs   # overwrites the solution with the scaffold
dotnet run                           # will fail — that's expected, fill in the TODOs
```

---

## Key concepts

### What a tool gives you
A tool is a callable action with a schema. It is best for verbs: search, validate, calculate, book, or save.

### What a skill adds

- **Identity** — a name and description in `SKILL.md` frontmatter
- **Instructions** — domain-specific guidance the agent can load when relevant
- **Resources** — reference files, examples, or scripts kept with the skill package
- **Progressive disclosure** — the agent starts with a small description, then loads detail on demand

## Anti-patterns to avoid

❌ **Turning every function into a skill** — if all you need is one callable action, use a tool.

❌ **Writing vague skill descriptions** — the agent uses the description to decide when to load the skill. Be specific about trigger scenarios.

❌ **Putting everything in `SKILL.md`** — keep core instructions concise and move detailed references into separate files.

❌ **Loading unreviewed skills** — skill instructions become agent context. Treat skills like dependencies and review them before use.

## References

- [Journey: Adding Skills](https://learn.microsoft.com/en-us/agent-framework/journey/adding-skills)
- [Agent Skills reference](https://learn.microsoft.com/en-us/agent-framework/agents/skills)
- [Agent Skills specification](https://agentskills.io/)

---

**→ Next: [Module 07 — Agents as Tools](../07-agents-as-tools/)**
