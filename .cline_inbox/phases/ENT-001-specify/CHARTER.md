# ENT-001 — Enterprise Specification & Plan

**Role:** Define
**Owner:** Platform Architecture
**Approver:** Enterprise Steering Committee
**Duration:** 3 weeks
**Exit:** Gate `gate-001-002`

## Objective

Lock every decision that constrains the platform's implementation. No code
written under this phase. The output is a fully-ratified architecture that
ENT-002 can implement without ambiguity.

## The bar for "locked"

A decision is locked when all five hold:

1. It is written down in an ADR
2. The ADR documents at least one dissenting opinion
3. It has been ratified by its designated approver
4. It is cited in `docs/ARCHITECTURE.md` or cross-referenced by another ADR
5. Reversing it requires a new ADR, not a conversation

"Probably fine" is not locked. If in doubt, it is not locked.

## Mandatory constraints

- **P1–P10** (see `.cline_inbox/PRINCIPLES.md`) apply to every decision. A
  decision that would violate any principle cannot be ratified.
- The one rule: **AI gathers evidence. Deterministic systems decide.**
- **Constraints before code.** This phase produces constraints. ENT-002
  implements them.
- **Two hashers, two concerns.** SHA-256 for storage addressing. Keccak256
  for on-chain Merkle nodes. This must be explicit in ADR-002 and ADR-006.

## Deliverables

| # | Deliverable | Location | Owner | Approver |
|---|---|---|---|---|
| D1 | Architecture document, ratified | `docs/ARCHITECTURE.md` | Platform Arch | Steering |
| D2 | ADR-001 Vertical slice modular monolith | `docs/adr/001-*.md` | Platform Arch | Arch Review Board |
| D3 | ADR-002 Storage port/adapter split + hashers | `docs/adr/002-*.md` | Platform Arch | Arch Review Board |
| D4 | ADR-003 MCP v2 stateless transport | `docs/adr/003-*.md` | Platform Arch | Arch Review Board |
| D5 | ADR-004 HybridCache default | `docs/adr/004-*.md` | Platform Arch | Arch Review Board |
| D6 | ADR-005 VerdictBinding not boolean | `docs/adr/005-*.md` | Compliance | Arch Review Board |
| D7 | ADR-006 Chain network selection | `docs/adr/006-*.md` | Platform Arch + Finance | Steering |
| D8 | ADR-007 Payment provider selection | `docs/adr/007-*.md` | Finance + Compliance | Steering |
| D9 | ADR-008 Registrar key custody | `docs/adr/008-*.md` | Security | Steering + CISO |
| D10 | ADR-009 Multi-tenancy model | `docs/adr/009-*.md` | Platform Arch | Steering |
| D11 | Project inventory reconciled | `AGENTS.md`, `.sln` | Platform Eng | Platform Arch |
| D12 | Service manifest updated | `service-manifest.yaml` | Platform Ops | Platform Arch |
| D13 | Image tags pinned | `docker-compose.yml` | Platform Ops | Platform Arch |
| D14 | Evidence index seeded | `.cline_inbox/manifests/evidence-index.yaml` | Program Mgmt | Steering |
| D15 | Baseline metrics captured | `bau/CONTINUOUS-IMPROVEMENT/benefits-realization.md` | Product | Steering |
| D16 | Phase charter reviewed and signed | this file | Program Mgmt | Steering |

## ADR required contents

Every ADR must include, in this order:

1. **Status** — Proposed / Accepted / Superseded
2. **Context** — the problem this solves, in plain language
3. **Decision** — the choice, in one sentence
4. **Consequences** — what this makes easier, what it makes harder
5. **Alternatives considered** — at least two, with rejection rationale
6. **Dissent** — documented opposing views and why they were overruled
7. **Reversibility** — how to reverse, and what it would cost
8. **References** — linked ADRs, external sources, related decisions

**ADRs without a dissent section are rejected at gate.** The dissent is what
forces intellectual honesty. An ADR with no dissent is an ADR that wasn't
argued.

## Decisions that must close in this phase

These four block downstream phases. They are not optional.

| # | Decision | ADR | Blocks | Fallback if not closed |
|---|---|---|---|---|
| 1 | Chain network (zkEVM vs PoS) | ADR-006 | ENT-002 step 11 | Extend phase. Do not proceed. |
| 2 | Payment provider (wakala capable) | ADR-007 | ENT-002 step 19 | Extend phase. Do not proceed. |
| 3 | Registrar key custody (KMS vs hot) | ADR-008 | ENT-003 audit scope | Extend phase. Do not proceed. |
| 4 | Multi-tenancy model | ADR-009 | ENT-005 | Provisional shared schema + RLS, upgrade path documented |

A "provisional" decision must include the upgrade path and the cost of
deferral. It is a decision to defer, not a decision to decide later.

## Exit gate criteria

All ten must pass. A failed criterion halts the program.

| # | Criterion | Evidence | Verifier |
|---|---|---|---|
| G1.1 | Architecture doc ratified with steering minutes | Signed PDF | Program Mgmt |
| G1.2 | Project count matches `dotnet sln list` | Diff output | Platform Eng |
| G1.3 | ADR-001 through ADR-005 merged, with dissent sections | Git log + file inspection | Arch Review Board |
| G1.4 | ADR-006 ratified | Signed ADR | Steering |
| G1.5 | ADR-007 ratified with vendor confirmation letter | Signed ADR + letter | Steering + Finance |
| G1.6 | ADR-008 ratified with security review | Signed ADR | Steering + CISO |
| G1.7 | ADR-009 ratified, or provisional model documented with upgrade path | Signed ADR | Steering |
| G1.8 | Zero `latest` or bare-major tags in compose or manifest | `grep -rE ':(latest\|[0-9]+)$'` empty | Platform Ops |
| G1.9 | Evidence index has ≥ 9 entries, all with named owners | `yq '.entries \| length'` ≥ 9 | Program Mgmt |
| G1.10 | Baseline metrics captured for all benefits targets | Filled table | Product |

## Falsification requirement

Before ratifying the architecture, an independent reviewer — not an author —
attempts to falsify each of the seven enforcement layers (P1–P7). They
document:

- One concrete attack per layer
- The result: **falsified** or **held**
- If falsified, the required fix before ratification

The architecture cannot pass `gate-001-002` until all seven layers hold.

The falsification report is filed at
`.cline_inbox/phases/ENT-001-specify/falsification-report.md` and is itself
an auditable artifact.

## Risks

| Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|
| Chain decision deferred to unblock | High | Critical | Timebox to 10 days; escalate to Steering | Platform Arch |
| PSP cannot meet wakala escrow | Medium | Critical | Parallel evaluation of 3+ providers | Finance |
| Registrar key custody rushed | Medium | Critical | Default to KMS; hot wallet only for Amoy testnet | Security |
| Multi-tenancy decision deferred | Medium | High | Provisional shared schema + RLS, upgrade path in ADR | Platform Arch |
| ADR dissent sections omitted | Medium | Medium | Gate check rejects ADRs without dissent | Arch Review Board |
| Baseline metrics not captured | High | Medium | Non-negotiable deliverable; ENT-009 depends on it | Product |
| Project count drift from earlier docs | High | Medium | `dotnet sln list` is authoritative; reconcile at gate | Platform Eng |

## RACI

R=Responsible, A=Accountable, C=Consulted, I=Informed

| Activity | Platform Arch | Enterprise Arch | Finance | Compliance | Security | Steering |
|---|---|---|---|---|---|---|
| Architecture doc | R | A | C | C | C | I |
| ADR-001 to 005 | R | A | I | C | C | I |
| Chain selection | R | C | A | C | C | I |
| PSP selection | C | I | R | A | C | I |
| Registrar key | C | I | I | C | R | A |
| Multi-tenancy | R | A | I | C | C | I |
| Image pinning | R | I | I | I | I | I |
| Baseline metrics | C | I | C | C | I | A |

## Out of scope

- Any code
- Any infrastructure provisioning
- Signed vendor contracts (selection only — commitment happens in ENT-002)
- Certification activities (ENT-003)

## Handoff to ENT-002

At phase close, Platform Engineering receives:

- Signed architecture document
- Nine ADRs with dissent sections
- Reconciled project inventory
- Pinned image tags
- Seeded evidence index
- Captured baselines
- Falsification report

Platform Engineering confirms receipt by executing:

```bash
.cline_inbox/hooks/gate-check.sh gate-001-002
```

A clean pass is the only valid receipt.
