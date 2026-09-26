# ADR-005 — VerdictBinding, not boolean, on Product

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Compliance, Platform Architecture
**Consulted:** Platform Engineering, Product
**Informed:** Steering Committee

## Context

The obvious way to model "is this product halal?" is a boolean: `Product.IsHalal`.
Most early sketches used this. It reads naturally. It is wrong.

The problem is structural. A boolean on `Product` can be set by anyone with
write access to the `Product` aggregate — a vendor through the marketplace
API, an admin through the back office, a bug in a migration. Every one of
those is a path to a halal claim that `tawheed` did not authorize.

The compliance requirement is stronger than "the value is usually right."
It is "the value is derived from a `tawheed` verdict, and only from a
`tawheed` verdict." A boolean cannot express that. It has no provenance.

## Decision

`Product` holds a **`VerdictBinding`**, not a boolean.

```csharp
public sealed record VerdictBinding
{
    public VerdictId Id { get; init; }
    public VerdictState State { get; init; }     // Issued | Expired | Revoked
    public DateTimeOffset DecidedAt { get; init; }
    public string PolicyVersion { get; init; }   // which policy decided
    public BlobRef EvidenceRoot { get; init; }   // Merkle root of evidence
    public string? AnchorTxHash { get; init; }   // on-chain anchor
    public Actor DecidedBy { get; init; }         // always the tawheed service
}
```

There is no `Product.IsHalal`. There is no `Product.HalalStatus`. There is
no boolean anywhere on the aggregate that represents halal compliance.

`ProductStatus` (Draft | PendingVerification | Active | ExpiringSoon |
Suspended | Rejected | Archived) is a **projection** of the `VerdictBinding`
and the certificate expiry date. Only the `Compliance` module may mutate
`ProductStatus`. No other module has the write path.

## Consequences

**Easier:**
- The provenance of every product's halal claim is queryable
- "Which policy version authorized this product?" is a join, not a hope
- "Show me the evidence behind this product" is a single traversal
- Audit trails are complete by construction
- Revoking a verdict is a state change, not a data loss

**Harder:**
- Listing a product requires a verdict lookup, which is slower than reading
  a boolean. Mitigated by caching the projection (not the binding).
- The aggregate is larger; more fields to maintain.
- Vendors cannot manually override. This is the point, but some support
  workflows assumed they could.
- Migrations that once touched one boolean now touch a record with several
  fields. More careful work.

## Alternatives considered

### Alternative A — `Product.IsHalal` boolean

Rejected. No provenance. Any write path is a violation.

### Alternative B — `Product.IsHalal` boolean + a separate `VerdictBinding`

Rejected. Redundant. Two sources of truth for one fact. They will drift.
The boolean becomes the thing everyone reads (fast), and the binding becomes
the thing nobody updates.

### Alternative C — Enum `Product.HalalStatus { Halal, Haram, Unknown }`

Rejected for the same reason as the boolean. An enum is a boolean with more
values. It still lacks provenance, and it still has a write path.

### Alternative D — VerdictBinding on a separate aggregate, not on Product

Considered. Put the binding in a `ProductCompliance` aggregate that
references `Product`. Rejected because it adds a hop for the most common
read path (product listing) and creates a synchronization problem: which
aggregate is authoritative when they diverge? Keeping the binding on
`Product` makes the relationship structural.

### Alternative E — Compute halal status on read from evidence

Rejected on cost grounds. Every product listing would invoke `tawheed`,
which is expensive. The binding records the *last* verdict; the certificate
expiry sweep and periodic revalidation keep it current.

## Dissent

**Product** argued for a boolean for vendor UX simplicity. Their position:
"Vendors just want to know if their product is live. The binding is complex
internal machinery."

**Overruled with mitigation.** The vendor UI displays `ProductStatus`, which
is a simple enum. The `VerdictBinding` is internal. Vendors never see it.
The complexity is in the domain, not the UX.

**Platform Engineering** argued for a cached boolean alongside the binding
for read performance. Their position: "Every product list query does a
join. That is expensive at scale."

**Accepted and documented as a risk, mitigated.** The projection caching
pattern (`CachingBehavior` on the read model, not the aggregate) means
lists read a cached view. The cache is invalidated when the binding
changes. The binding itself is not queried on the hot path.

**Security** raised a concern about the `DecidedBy` field being forgeable.
Their position: "If an attacker can set `DecidedBy` to `tawheed`, they
have a false verdict."

**Accepted and mitigated three ways.** (1) The `Compliance` module is the
only code path that creates a `VerdictBinding`, enforced by architecture
test. (2) The value of `DecidedBy` is always the `tawheed` service identity,
which is authenticated at the network layer. (3) Verdicts are anchored
on-chain; a forged binding would not have a valid anchor and would fail
verification.

**One dissent not held:** the initial proposal was to make `VerdictBinding`
nullable to represent "no verdict yet." This was rejected. The `Draft`
state of `ProductStatus` handles "no verdict yet" more cleanly than a null
field. Nullable verdicts introduce null-check bugs everywhere.

## Reversibility

**Cheap to extend, expensive to remove.** Adding fields to `VerdictBinding`
is a normal migration. Removing the binding and replacing with a boolean
would require:

1. A migration that collapses the binding to a boolean (losing provenance)
2. An architecture test reversal
3. Audit trail adjustments for compliance

This is technically possible but a compliance regression. We would not do
it. Treat as effectively irreversible.

## References

- `.cline_inbox/PRINCIPLES.md` — P4 (only Compliance mutates ProductStatus)
- `HalalChain.Domain/Halal/ValueObjects/VerdictBinding.cs`
- `HalalChain.Platform.Api/Modules/Compliance/StateMachine/ProductStatusMachine.cs`
- `HalalChain.Architecture.Tests/Rules/ModuleBoundaryRules.cs` — enforces P4
