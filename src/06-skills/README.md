# Module 06 — Skills

**Concept:** Package reusable domain expertise — instructions, reference material, and scripts — into self-contained units any agent can discover and use.

## What you'll learn

- What a skill is and how it differs from a tool
- How `SKILL.md` defines a skill's identity and instructions
- How progressive disclosure keeps the context window lean
- How `AgentSkillsProvider` makes skills available to an agent

## Package note

This sample uses `Microsoft.Agents.AI.Skills` in the project file as requested. Because Agent Framework packages are evolving quickly, verify the exact latest NuGet package name and version before restoring.

## When to use skills

Add skills when:
- A **cluster of related knowledge** logically belongs together (expense policy, code review guidelines)
- **Multiple agents** need the same domain expertise (single source of truth)
- You want to **share capabilities** across teams or projects as portable packages
- You need **context efficiency** — skills load detail only when the agent actually needs it

## Tools vs. Skills

| | Tool | Skill |
|--|------|-------|
| **What it provides** | A single callable action | Instructions + reference material + optional scripts |
| **Context cost** | Schema always in the prompt | Only name/description upfront (~100 tokens) |
| **Best for** | Individual actions (search, book, validate) | Domain expertise (expense policy, onboarding) |

> Think of tools as **verbs** (search, book, validate) and skills as **expertise** (travel booking knowledge, expense policy knowledge).

## How progressive disclosure works

```
Stage 1: Advertise  — Agent sees skill name + description (~100 tokens)
Stage 2: Load       — Agent calls load_skill to get full SKILL.md (<5000 tokens recommended)  
Stage 3: Resources  — Agent calls read_skill_resource for supplementary files (on demand)
Stage 4: Scripts    — Agent calls run_skill_script to execute bundled code (on demand)
```

This means 10 registered skills add ~1,000 tokens of overhead — not 50,000.

## Skill structure

```
skills/
└── expense-report/        ← directory name = skill name
    ├── SKILL.md           ← required: frontmatter + instructions
    ├── references/
    │   └── POLICY_FAQ.md  ← loaded on demand with read_skill_resource
    └── scripts/
        └── validate.py    ← executed on demand with run_skill_script
```

## Running this module

```bash
cd src/06-skills
dotnet run
```

## Anti-patterns to avoid

❌ **Overly broad skills** — A skill that covers too many domains becomes unfocused. Keep skills scoped to one domain.

❌ **Very long SKILL.md** — Keep instructions concise (<500 lines). Move detailed reference material to separate resource files.

❌ **Skipping security review** — Skill instructions are injected into the agent's context. Treat skills like third-party dependencies — review them before deploying.

## References

- [Journey: Adding Skills](https://learn.microsoft.com/en-us/agent-framework/journey/adding-skills)
- [Agent Skills reference](https://learn.microsoft.com/en-us/agent-framework/agents/skills)
- [Agent Skills specification](https://agentskills.io/)
---

**→ Next: [Module 07 — Agents as Tools](../07-agents-as-tools/)**
