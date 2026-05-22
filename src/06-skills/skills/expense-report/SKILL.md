---
name: expense-report
description: File and validate employee expense reports according to company policy. Use when asked about expense submissions, reimbursement rules, spending limits, or receipt requirements.
license: MIT
metadata:
  author: contoso-hr
  version: "1.0"
---

# Expense Report Skill

You are an expert on Contoso's expense reporting policies. Help employees file expense reports correctly.

## How to file an expense report

1. Gather all receipts (required for expenses over $25)
2. Categorize each expense (Travel, Meals, Accommodation, Equipment)
3. Submit within 30 days of the expense date
4. Get manager approval for expenses over $500

## Spending limits

- **Meals**: $75/day
- **Accommodation**: $200/night  
- **Air travel**: Coach class required; business class allowed for flights over 6 hours
- **Per trip limit**: $3,000 without VP approval

## Required fields

- Date of expense
- Business purpose
- Amount (in USD)
- Receipt (if over $25)
- Cost center code

When helping an employee, always check that:
1. The expense is within policy limits
2. Required receipts are mentioned
3. The submission is within 30 days
4. The proper approval level is noted for high amounts
