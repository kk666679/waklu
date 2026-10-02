# ADR-010 — Unified browser automation engine abstraction

**Status:** Accepted
**Date:** 2026-10-02
**Deciders:** Platform Architecture, Platform Engineering
**Consulted:** Security, Compliance
**Informed:** Steering Committee

## Context

The platform needs browser-driven data collection: logging into an external
portal, navigating to a certificate page, and capturing what the page says as
**evidence**. Two mature automation stacks exist — Playwright and Selenium —
and each has reasons to be chosen. Neither exists in this repository today.

The instinct is to pick one and call the work done. That is wrong for two
reasons:

1. **The workflow engine must be engine-agnostic.** A workflow node says
   "fill this field, click this button, capture the result." It must not say
   "`IWebElement.Click()`". If the node type is coupled to one vendor, the
   vendor becomes load-bearing for the domain, and swapping it becomes a
   rewrite.
2. **Different targets favour different stacks.** Modern pages and headless
   CI favour Playwright. Legacy/older-auth portals and existing enterprise
   test suites favour Selenium. Both will be true in one fleet.

`HalalChain.Automation` is already the platform's scheduler (README:
"Automation is a scheduler. It never decides."). Browser automation is a
long-running, lock-protected, telemetry-emitting unit of work — precisely the
shape `IScheduledJob` already models. Building a second execution host would
fork the lock, telemetry, and health-check story.

## Decision

**One abstraction, two adapters, one existing host.**

```
Workflow node → AutomationExecutionContext → IBrowserAutomationEngine
                                                   │
                          ┌────────────────────────┴───────────────────────┐
                PlaywrightBrowserAutomationEngine            SeleniumBrowserAutomationEngine
                          └────────────────────────┬───────────────────────┘
                                             IBrowserSession
```

1. **`IBrowserAutomationEngine`** is the only type workflow code sees. It
   opens an `IBrowserSession`; it never names a vendor.
2. **`IBrowserAutomationEngineResolver`** maps a `BrowserEngineKind`
   (`Playwright` | `Selenium` | `Default`) to an engine, so a workflow — or an
   operator via config — selects the stack per run without a code change.
   `Default` is resolved from configuration, not hard-coded, so the fleet has
   exactly one documented default at a time.
3. **Adapters own the translation.** `BrowserAction`/`BrowserLocator` are our
   neutral vocabulary; each adapter is the single place that translates them
   into vendor calls. Translating twice is a known cost; embedding one vendor
   in the domain is a known un-cost.
4. **Everything else is reused, not rebuilt:**

| Need | Reuses |
|---|---|
| Trigger, queue, concurrency, cluster lock | `JobScheduler`, `IScheduledJob`, `IDistributedLock` (Npgsql advisory lock) |
| Long-running execution host | `HalalChain.Automation` worker |
| Correlation, clock, logging, scope | `JobContext` |
| Captured artefacts (screenshots, HTML, traces) | `IEvidenceStore` → `IBlobStore` (SHA-256, append-only) |
| Telemetry | `JobTelemetry` / OpenTelemetry meter already wired |
| Secrets | reference only — `credentialId`, never an inline password |

5. **Records all the way down.** `BrowserAction`, `BrowserLocator`,
   `AutomationExecutionContext`, `AutomationNodeResult` are immutable records,

## Consequences

**Easier:**
- Workflow nodes are portable across engines and testable with a fake engine.
- Adding a third engine (e.g. a managed service) is one class + one resolver
  entry, with no workflow change.
- Browser runs inherit the scheduler's lock, telemetry, and health check for
  free rather than re-deriving them.
- Evidence capture composes with the existing append-only store, so browser
  artefacts get the same immutability and retention guarantees as everything
  else.

**Harder:**
- Two adapters must be kept in step as actions are added. A new action kind
  means two translations and two test suites. This is the real, recurring cost
  of the abstraction and it is accepted deliberately.
- Vendor-only features (Playwright's network interception, Selenium's
  devtools) do not fit the neutral vocabulary. They must be requested as new
  neutral action kinds or they will not be available.
- The adapter seam means some failures surface only at translation time, not
  at compile time.

## Alternatives considered

### Alternative A — Pick Selenium only
Rejected. Cheapest today, but the domain would be permanently coupled to a
vendor that is not the better fit for modern pages or headless CI.

### Alternative B — Pick Playwright only
Rejected for the mirror reason. Existing enterprise portal targets in the
fleet are documented as Selenium-friendly; a single choice would fail there.

### Alternative C — Abstract over both with a "least common denominator"
Considered and rejected as stated. A union of the lowest-level primitives of
both libraries produces an abstraction that is neither library's vocabulary.
We do the opposite: the vocabulary is **task-shaped** (navigate, locate, act,
capture), defined by our own use cases, and each adapter maps up into it.

### Alternative D — Put browser automation in `HalalChain.Agents`
Rejected. `HalalChain.Agents` is the eval harness; it is read-only by
construction ("never writes to evidence storage"). Browser automation writes
evidence. Wrong home.

### Alternative E — A second worker/host for browser jobs
Rejected. Forks advisory-lock acquisition, telemetry, and health reporting.

### Alternative F — Let nodes call Playwright/Selenium directly
Rejected. Couples workflow definitions to a vendor at the type level and makes
per-run engine selection impossible.

## Dissent

**Platform Engineering** argued for Playwright-only: "Two adapters is double
the maintenance, and we have no Selenium code to migrate."
**Partially overruled.** The double-maintenance cost is real and accepted
above. The premise — nothing to migrate — is exactly why the seam should be
drawn now, while it is cheap, rather than after the vendor is in fifty nodes.

**Security** asked whether one engine should be disallowed for privileged
targets. **Accepted, and addressed in ADR-011**, which makes engine choice and
egress policy orthogonal controls.

## Reversibility

**Cheap to add, expensive to remove.** Adding an engine is additive. Dropping
one is deleting a class and a resolver entry. Collapsing to a single vendor
would require rewriting nodes and is discouraged; treat the abstraction as
effectively permanent. The resolver indirection is the reversibility mechanism.

## References

- `.cline_inbox/PRINCIPLES.md` — P1 (agents never decide), append-only storage
- `HalalChain.Automation/` — `JobScheduler`, `IScheduledJob`, `IDistributedLock`
- `HalalChain.Application/Storage/IEvidenceStore.cs`
- ADR-002 — storage port/adapter split (this is the same pattern, applied)
- ADR-011 — browser automation security and tenancy constraints

   so an action list is a value that can be logged, diffed, replayed, and
   serialised to the workflow designer without hand-written copy logic.
