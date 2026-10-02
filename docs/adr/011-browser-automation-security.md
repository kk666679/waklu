# ADR-011 — Browser automation security and tenancy constraints

**Status:** Accepted
**Date:** 2026-10-02
**Deciders:** Platform Architecture, Security
**Consulted:** Compliance, DPO, Platform Engineering
**Informed:** Steering Committee

Companion to ADR-010. ADR-010 decides *how* browser automation is structured;
this ADR decides *what it is permitted to do*. Neither is useful alone: an
engine abstraction without an egress policy is an SSRF primitive with a nice
interface.

## Context

A browser automation engine is, functionally, a server-side process that
fetches attacker-influenced URLs and executes attacker-influenced page
content. Three things make it dangerous in a compliance platform:

1. **SSRF.** A workflow that navigates to a "URL from a field" can be pointed
   at `http://169.254.169.254/` (cloud metadata), `http://localhost:5001/`
   (our own API, which trusts internal callers), or the Postgres/Neo4j/Qdrant
   ports. The browser follows redirects, so an allowlist checked once at
   submit time is not enough.
2. **Credential exposure.** Portal logins need usernames and passwords. If a
   workflow definition can carry them inline, they land in workflow JSON, in
   git, in logs, and in the designer's export file — four disclosure paths.
3. **Verdict laundering.** If a browser node can write to `ProductStatus` or
   mint a `VerdictBinding`, then a scraped page becomes an authority over
   halal compliance. `.cline_inbox/PRINCIPLES.md` P1 and ADR-005 exist
   precisely to stop this, and browser automation must not open a side door.

## Decision

### D1 — Egress allowlist, enforced per navigation

Navigation targets must match a configured allowlist (`Browser:AllowedHosts`),
matched on **host** with optional port, plus an explicit deny of link-local,
loopback, and private ranges unless a host is named in the allowlist.

Enforcement happens in the adapter **immediately before every navigation**,
not once per session — a page cannot pivot the session to an internal host,
because each navigation is re-checked. Redirects are re-checked for the same
reason.

Deny-by-default: no allowlist configured ⇒ navigation throws. A missing
security control must fail closed, never degrade to "browse anything."

### D2 — Credentials by reference only

`BrowserAction` carries `CredentialId`, never a secret value. Resolution goes
through the existing credential/certificate ports at execution time; the
plaintext exists only inside the executing process, never in the workflow
definition, the designer export, telemetry, or the evidence record.

This makes workflow definitions safe to store, review, and export — the
credential is a foreign key to something already governed.

### D3 — Browser outputs are evidence, never verdicts

Screenshots, HTML snapshots, and extracted field values are ingested through
`IEvidenceStore` as ordinary evidence with an actor, kind, and tags. Nothing
in `HalalChain.Automation` writes `ProductStatus`, creates a
`VerdictBinding`, or emits any field named `Verdict`, `IsHalal`, `Approved`,
or `Rejected` — the same rule `EvidenceProposal` documents, applied to a new
subsystem. A browser run can *support* a decision; it can never *be* one.

The only write path from a browser run is append-only evidence ingest.

### D4 — Per-tenant session isolation

Sessions are scoped by tenant: separate browser context, separate storage
state, no cookie/localStorage sharing across tenants. Per ADR-009, v1 runs the
Standard tier (shared host, isolated context); the abstraction leaves room for
a dedicated-host tier later because isolation lives in the session factory,
not in the workflow.

A session may only be opened with an `ExecutionContext` carrying a tenant
identifier. No tenant ⇒ no session.

### D5 — Headless, sandboxed, non-privileged

Browsers run headless, with a non-root user and no host filesystem writes
outside the artefact directory. Downloads and artefacts go to memory/temp and
are streamed into `IEvidenceStore`, not left on disk.

### D6 — Execution is bounded

Every run is bounded by timeout, maximum navigation count, and maximum action
count, taken from `Browser:Execution` options. Unbounded browser loops are a
denial-of-service and a data-exfiltration risk; both are bounded by config,
not by hope.

### D7 — Every run is attributable

Runs carry the existing `JobContext.CorrelationId`, emit through
`JobTelemetry`, and record engine kind, host, action count, duration, and
outcome. The evidence descriptor names the actor. This is the same audit
posture as every other job — no separate logging path to get wrong.

## Consequences

**Easier:**
- The allowlist is the single control that turns SSRF from "possible" into
  "configurable and testable."
- Workflow definitions are safe to store and export because they cannot
  contain secrets.
- Browser evidence inherits immutability, retention, and auditability instead
  of needing bespoke versions of each.

**Harder:**
- Every new host must be allowlisted before a workflow can reach it — an
  operational step, not a code step. Accepted; that friction is the control.
- Splitting credential resolution from action definition means a run can fail
  on a missing credential at execution time rather than at author time.
  Accepted, and surfaced as a distinct error kind.
- Per-tenant context isolation adds a small per-session cost.

## Alternatives considered

### Alternative A — Allowlist checked once when the workflow is saved
Rejected. Redirects and page-driven navigation make submit-time checks
insufficient. The check must sit in front of every navigation.

### Alternative B — Allow secrets inline in workflow definitions "encrypted at rest"
Rejected. Encryption at rest does not help once the definition is exported,
logged, or read by a process holding the key — and the designer export path
guarantees the definition leaves the database.

### Alternative C — Browser runs may update compliance status directly
Rejected. Violates P1 and ADR-005. A scraped page is not an authority.

### Alternative D — Block all private ranges outright, no exception
Considered. Rejected as the *only* mechanism because some deployments must
reach an internal portal; the allowlist can name that host explicitly, which
is strictly more auditable than a blanket rule with an undocumented bypass.

## Dissent

**Compliance** asked for full page recordings on every run as evidence.
**Deferred.** Screenshots, HTML, and extracted values are in scope; video is
not, on storage-cost grounds. Revisit if a regulator requires it.

**Security** wanted deny-all private ranges with no allowlist escape.
**Partially accepted.** The allowlist escape remains, but only via explicit
host configuration, which is itself an auditable artifact.

## Reversibility

Controls are additive. Removing the allowlist or allowing inline secrets would
be a deliberate security regression requiring a new ADR — treat as
effectively irreversible for the same reason ADR-005's binding is.

## References

- ADR-005 — VerdictBinding, not boolean
- ADR-009 — Multi-tenancy model
- ADR-010 — Unified browser automation engine abstraction
- `.cline_inbox/PRINCIPLES.md` — P1, P3, P4
- `HalalChain.Application/Storage/IEvidenceStore.cs`, `IAccessLog.cs`

