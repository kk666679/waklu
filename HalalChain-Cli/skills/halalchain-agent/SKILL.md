---
name: halalchain-agent
description: Use the HalalChain CLI agent mode to query platform data and explain policy outcomes. Use when an operator asks about vendors, certificates, evidence, or policy evaluations.
metadata:
  sources:
    - README.md
---

# HalalChain Agent Mode

Run `halalchain agent "<prompt>"` to query the platform through MCP.

## What the agent can do

- Call `VendorLookupTools` to find vendors by name, ID, or certificate number
- Call `HalalEvidenceTools` to retrieve evidence records and signed URLs
- Call `PolicyEvaluationTools` to ask tawheed for a verdict on a specific case

## What the agent must never do

The agent never assigns a halal verdict itself. If a user asks "is this vendor halal?",
the agent calls `EvaluatePolicyTool` and reports tawheed's output. If it answers from
its own reasoning, that is a bug.

## Common mistakes

- Running the agent with `HALALCHAIN_MCP_URL` unset. The CLI fails at startup
  with a clear error. Check the env var before assuming the agent is broken.
- Expecting the agent to write evidence. MCP tools exposed to the CLI are read-only.
  Writes go through the platform API, gated by the Compliance module.
- Interpreting "no verdict available" as "not halal". These are different.
