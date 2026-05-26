# Start Here

You've completed the prerequisites. You're ready to build TripBot.

## Before you write any code — fork & branch

You'll be modifying code throughout this course. Fork the repo first so your changes are yours, then create a working branch.

**Fork on GitHub:**
1. Click **Fork** in the top-right corner of the repo page on GitHub
2. Clone your fork locally:
```bash
git clone https://github.com/<your-username>/learn-agent-framework-csharp.git
cd learn-agent-framework-csharp
```

**Create a working branch:**
```bash
git checkout -b my-learning
```

Now all your changes stay on `my-learning` and the original `main` is always there as a clean reference if you need to look something up or reset.

---

## What you're building

TripBot is a travel planning assistant that evolves across 10 modules. Each module adds exactly one capability — no more, no less. By the end you'll have a fully hosted, multi-agent travel assistant exposed over the A2A protocol.

```
Module 01 → Hello Agent         (TripBot answers its first question)
Module 02 → Tools               (TripBot checks weather and local time)
Module 03 → Multi-Turn          (TripBot remembers your trip context)
Module 04 → Memory              (TripBot remembers your preferences)
Module 05 → Middleware          (TripBot gets logging and guardrails)
Module 06 → Skills              (TripBot knows visa requirements)
Module 07 → Agents as Tools     (TripBot delegates to a weather specialist)
Module 08 → Agent-to-Agent      (TripBot calls a remote agent over HTTP)
Module 09 → Workflows           (TripBot runs a deterministic pipeline)
Module 10 → Hosting             (TripBot goes live as a hosted service)
```

## → [Begin with Module 01 — Hello Agent](01-hello-agent/)

Each module's `README.md` explains:
- **What you'll learn** — the concept being introduced
- **When to use it** — so you can apply this in your own projects  
- **Trade-offs** — what you give up by choosing this pattern
- **Anti-patterns** — common mistakes to avoid
- **→ Next module** — so you always know where to go next
