# `.autoclaw/skills/`

Skills are the only executable artifacts in the fleet that were authored rather
than derived from a schema. That makes them the most likely place for something
to get in that should not, so the directory is split by governance state and
only one of the four is executable.

| Directory | State | Executable |
|-----------|-------|-----------|
| `published/` | passed every check in `governance/skills/checks/` | yes |
| `staging/` | under review | **no** — inert |
| `suspended/` | withdrawn, retained for forensics | **no** |
| `implementations/` | the code the manifests reference | yes |

## Why `staging/` is inert rather than merely unlisted

A skill in staging that is not yet executable, and a skill that is executable
but has not been announced, are the same file to anyone debugging at 2am. Making
staging genuinely inert removes the question. A reference to a staging skill
from an agent persona is a load-time failure, not a warning.

## The five checks

`governance/skills/checks/` — manifest validation, capability review, static
security, no-verdict scan, dependency review. A skill passes all five or it
does not publish. The no-verdict scan is the one this platform cares about
most, and `skills-reviewer` is the agent that runs it.

## Manifests

Every directory under `published/` needs a `manifest.yaml` declaring tools,
capabilities, what it emits, what it must never emit, a budget, and its
governance record. `AutoclawStructureTests.Every_Skill_Has_A_Manifest` fails
the build on a directory without one, and
`AutoclawStructureTests.Published_Skills_Do_Not_Emit_Verdicts` fails on one
whose `does_not_emit` is missing `ComplianceVerdict`.

## What a skill may emit

`EvidenceProposal`, `EvidenceFinding`, `RoutingRecommendation`, `ReviewReport`.

Not `ComplianceVerdict`. Not a certificate. Not a state transition. A skill that
wants to conclude something is describing a rule, and rules live in
`compliance/rules/` where tawheed evaluates them deterministically.
