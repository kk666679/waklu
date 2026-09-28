---
name: halalchain-evaluate
description: Use the HalalChain CLI to request a policy evaluation from tawheed. Use when an operator needs to know whether a case satisfies halal requirements.
metadata:
  sources:
    - docs/ARCHITECTURE.md
---

# HalalChain Policy Evaluation

Run `halalchain evaluate --vendor <id> --product <sku>` to ask tawheed for a verdict.

## What this does

Forwards the case to the deterministic policy engine. Returns tawheed's verdict:
halal, not halal, or insufficient evidence. If the verdict is insufficient,
the output lists which policy requirements lack evidence.

## What this does not do

The CLI does not evaluate policy. It does not cache verdicts. It does not
infer a verdict from evidence. If tawheed is unreachable, the command fails
rather than guessing.

## Common mistakes

- Assuming a "halal" verdict is permanent. Verdicts expire with the
  certificate. Re-run `halalchain evaluate` to get the current state.
- Treating "insufficient evidence" as "not halal". They are different.
  The gap agent can propose what evidence is missing.
