# HalalChain Agent Principles

The single source of truth for the agent fleet. Parsed by
`halalchain_get_principles`. Every rule elsewhere in `.autoclaw/` traces back
to an identifier here; a rule that cannot cite one does not belong in the tree.

The first three are load-bearing. The rest tighten around them.

| ID  | Principle |
|-----|-----------|
| P1  | **Evidence before decision** |
| P2  | **Deterministic compliance** — tawheed is the only decision authority |
| P3  | **AI does not decide** — propose, never conclude |
| P4  | **Provenance matters** — source, issuer, scope, hash, chain-of-custody |
| P5  | **External participants are untrusted by default** |
| P6  | **Tenant isolation is mandatory** |
| P7  | **Secrets are never exposed** — logs, traces, exceptions, DB, telemetry |
| P8  | **Capability over assumption** — declared and enforced, never inferred |
| P9  | **Governance is explicit** — authority ≠ technical access |
| P10 | **Blockchain is integrity, not authority** |
| P11 | **BYOK credentials are never logged** |
| P12 | **Skills cannot bypass governance** |
| P13 | **Providers contribute evidence and capabilities, not verdicts** |
| P14 | **Delegated authority is explicit, scoped, revocable, auditable** |
| P15 | **Provider capabilities are declared and enforced** |

## What each one costs in structure

- **P1** — `evidence/` is a top-level domain, not a subfolder of `agents/`. An
  agent that wants to propose something must cross a domain boundary to do it,
  and the crossing is auditable.
- **P2** — `tawheed/` is sealed. `MANIFEST.yaml` declares `writers: []`.
  `tawheed/SEALED` is a physical marker checked by an architecture test.
- **P3** — there is no `verdicts/` or `decisions/` directory to write to, and
  the strings `SetVerdict`, `WriteVerdict`, `OverrideTawheed`,
  `ApproveCertificate`, `RejectCertificate` are blocked by the pre-commit gate
  everywhere outside `tawheed/`.
- **P4** — every evidence entity carries `provenance.{json,md}` next to its
  state file. Provenance is not a field on the record; it is a sibling file.
- **P5** — ingress goes through `security/trust/ingress-rules.yaml` and the
  `connectors/<provider-class>/` folders. No agent reaches a provider directly.
- **P6** — `tenants/` is a first-class domain and `contracts/tenancy.schema.json`
  is a published contract, not a table comment.
- **P7** — `byok/` stores sealed references, never values.
  `observability/redaction.yaml` consumes `security/secrets/never-log.yaml`.
- **P9** — `governance/` holds the certification and skill state machines. A
  role's authority is a property of the machine, not of the caller's token.
- **P10** — the directory is `anchors/`, not `blockchain/`. Its only output is a
  hash receipt (`anchors/verification/receipt-verifier.md`). Nothing reads a
  compliance status back out of a chain.
- **P12** — `skills/published/` is read-only and reachable only through the
  governance machine in `governance/skills/`. `skills/staging/` is not
  executable.

## Adding a principle

Extend the table, then add the structural consequence to `MANIFEST.yaml` and a
test to `HalalChain.Architecture.Tests/Rules/AutoclawStructureTests.cs`. A
principle with no enforcement is a comment.
