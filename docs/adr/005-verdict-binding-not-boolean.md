# ADR-005 — VerdictBinding, not boolean, on Product

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Compliance, Platform Architecture
**Consulted:** Platform Engineering, Product

## Context

The obvious model for "is this product halal?" is `Product.IsHalal`. It
reads naturally. It is wrong.

A boolean on `Product` can be set by anyone with write access — a vendor
through the marketplace API, an admin through the back office, a bug in a
migration. Every one of those is a path to a halal claim `tawheed` did not
authorize. The compliance requirement is not "the value is usually right."
It is "the value is derived from a `tawheed` verdict, and only from a
`tawheed` verdict." A boolean cannot express that. It has no provenance.

## Decision

**`Product` holds a `VerdictBinding`.**

```csharp
public sealed record VerdictBinding
{
    public VerdictId Id { get; init; }
    public VerdictState State { get; init; }        // Issued | Expired | Revoked
    public DateTimeOffset DecidedAt { get; init; }
    public string PolicyVersion { get; init; }      // which policy decided
    public BlobRef EvidenceRoot { get; init; }      // Merkle root of evidence
    public string? AnchorTxHash { get; init; }      // on-chain anchor
    public Actor DecidedBy { get; init; }           // always the tawheed service
}
```

There is no `Product.IsHalal`. No `Product.HalalStatus`. No boolean
anywhere on the aggregate representing halal compliance.

`ProductStatus` (Draft | PendingVerification | Active | ExpiringSoon |
Suspended | Rejected | Archived) is a **projection** of the `VerdictBinding`
and certificate expiry. Only `Modules/Compliance/` mutates it.

## Consequences

**Easier:** Provenance is queryable. "Which policy version authorized this?"
is a join. "Show me the evidence" is a single traversal. Audit trails are
complete by construction.

**Harder:** Listing requires a verdict lookup — slower than a boolean.
Mitigated by caching the projection, not the binding. Vendors cannot
manually override. Migrations touching the binding are more careful.

## Alternatives considered

**`Product.IsHalal` boolean.** Rejected. No provenance. Any write path is
a violation.

**Boolean plus a separate `VerdictBinding`.** Rejected. Two sources of
truth. The boolean becomes the fast path everyone reads; the binding
becomes stale.

**Enum `HalalStatus { Halal, Haram, Unknown }`.** Rejected. A boolean with
more values. Same problems.

**VerdictBinding on a separate aggregate.** Considered. Adds a hop for the
common read. Creates a synchronization question: which is authoritative
when they diverge?

**Compute on read from evidence.** Rejected on cost. Every listing would
invoke `tawheed`.

## Dissent

**Product** wanted a boolean for vendor UX simplicity. *Overruled with
mitigation:* the vendor UI displays `ProductStatus`, a simple enum. The
binding is internal. Complexity is in the domain, not the UX.

**Platform Engineering** wanted a cached boolean alongside the binding.
*Accepted as a risk, mitigated:* the projection is cached
(`CachingBehavior` on the read model). The binding itself is not queried
on the hot path.

**Security** raised that `DecidedBy` is forgeable. *Mitigated three ways:*
(1) only `Compliance` creates bindings, enforced by arch test; (2)
`DecidedBy` is always the authenticated `tawheed` service identity; (3)
anchors make forgery detectable.

**One dissent not held:** nullable `VerdictBinding` for "no verdict yet."
*Rejected:* `Draft` state handles it more cleanly. Nullable verdicts
introduce null checks everywhere.

## Reversibility

Cheap to extend (new fields are migrations). Expensive to remove — would
require collapsing to a boolean (losing provenance) and reversing arch
tests. Treat as effectively irreversible.

## References

- `.cline_inbox/PRINCIPLES.md` — P4
- `HalalChain.Domain/Halal/ValueObjects/VerdictBinding.cs`
- `HalalChain.Platform.Api/Modules/Compliance/StateMachine/ProductStatusMachine.cs`
