# `tawheed/` — SEALED deterministic boundary

This directory is the only place a compliance decision is produced. It is
sealed: no agent, skill, connector, MCP tool, or workflow writes here.

`MANIFEST.yaml` declares `writers: []`. The `SEALED` marker in this directory
is asserted by
`HalalChain.Architecture.Tests.Rules.AutoclawStructureTests.Tawheed_Directory_Is_Sealed`,
and the pre-commit hook refuses any verdict-authority identifier appearing
outside this path.

## Why a directory

Sealing the boundary as a *filesystem property* rather than a review
convention is the point. An architectural rule enforced in review is enforced
until the reviewer is busy. A rule enforced by the shape of the tree is enforced
always, and it is enforced before the code compiles.

## What is inside

| Path | Role |
|------|------|
| `SEALED` | marker file; must exist |
| `api/evaluate.md` | the single authoritative decision call |
| `api/explain.md` | human-readable explanation of a prior result |
| `api/capabilities.md` | what this boundary is permitted to do |
| `policies/core.yaml` | base policy set |
| `policies/jurisdiction-overrides.yaml` | per-jurisdiction amendments |
| `policies/scheme-overrides.yaml` | per-scheme amendments |
| `request.schema.json` | `TawheedRequest` |
| `response.schema.json` | `TawheedResponse` |
| `audit/evaluation-log.md` | audit record shape and retention |
| `audit/retention.yaml` | retention policy |

## Who may call

`workflows/*` only. Not agents, not skills, not MCP.

That is narrower than it looks. An agent's job ends at `EvidenceProposal`.
The workflow collects proposals, packages them, and is the only thing that
holds the `tawheed.evaluate` capability.

## Guarantees

- **Deterministic.** Same request bytes → same response bytes. No sampling, no
  temperature, no wall-clock dependence other than the explicit `evaluated_at`
  the request carries.
- **Auditable.** Every call is logged with the request hash and the policy
  version in force. A result whose policy version cannot be identified is not
  reproducible and must not be relied on.
- **No side effects** beyond the audit write.
- **Fails closed.** Missing evidence, expired evidence, or an unrecognised
  policy version produce a non-compliant or error result, never a permissive
  default.

## Reading

The Policy Engine is the deterministic compliance core of the platform. In
`.halalchain/tawheed/` it is a FastAPI service; this directory is its contract,
its policy set, and its audit policy. The two must be changed together.
