# ADR-007 — Payment provider selection

**Status:** Accepted — vendor selection deferred to procurement
**Date:** 2026-09-26
**Deciders:** Finance, Compliance
**Consulted:** Shariah Advisory Board, Legal, Platform Architecture

## Context

The marketplace requires a payment provider. Most providers assume a
conventional fee model where the platform holds buyer funds and pays
vendors later, often with interest-bearing settlement. This is incompatible
with the halal-finance constraints.

Constraints (from `docs/ARCHITECTURE.md` §7.4):

- **No riba.** No interest on delayed payouts. Revenue is fixed service fee
  (ujrah), disclosed at listing.
- **No gharar.** Delivery terms stated before capture.
- **Wakala-based escrow.** Funds held under documented agency.
- **Zakat and sadaqah pass-through.** Separate line item, no commingling.

## Decision

**Port-first. Vendor deferred to procurement during ENT-002.**

```csharp
public interface IPaymentProvider
{
    Task<Authorization> AuthorizeAsync(AuthorizationRequest req, CancellationToken ct);
    Task<Capture> CaptureAsync(AuthorizationId authId, Money amount, CancellationToken ct);
    Task<Split> SplitAsync(CaptureId capId, IReadOnlyList<SplitAllocation> allocs, CancellationToken ct);
    Task<Payout> ReleaseAsync(SplitId splitId, CancellationToken ct);
    Task<Refund> RefundAsync(CaptureId capId, Money amount, Reason reason, CancellationToken ct);
    Task<EscrowStatus> GetEscrowStatusAsync(CaptureId capId, CancellationToken ct);
}
```

No interest-related methods. No "hold funds and pay later" concept that
assumes a fee. Wakala escrow is structural.

### Procurement criteria (mandatory before weighting)

| # | Criterion | Mandatory |
|---|---|---|
| 1 | Wakala escrow structure, native or contractual | Yes |
| 2 | No interest on delayed payouts, contractually | Yes |
| 3 | Split payment, per-vendor allocation | Yes |
| 4 | Zakat/sadaqah pass-through without commingling | Yes |
| 5 | SAQ-A eligible (no card data in platform scope) | Yes |

Weighted criteria: API quality (5%), multi-currency (5%), fee transparency
(5%). Fee is one criterion of eight, not the sole determinant.

**Deadline:** ENT-002 week 4, or `Payments` module (build step 31) cannot
proceed.

## Consequences

**Easier:** `Payments` can be built against a stub before vendor selection.
Switching providers is adapter code, not a rewrite. Halal-finance
constraints live in the interface, not provider-specific code.

**Harder:** The stub must correctly model wakala semantics, which requires
legal review of what "wakala" means in the interface. Adapter code will
be needed regardless. Missing the deadline delays ENT-002.

## Alternatives considered

**Select vendor first, then design the port.** Rejected. Inverts the
dependency. The port should be shaped by domain constraints, not a vendor's
API.

**Bespoke escrow system.** Rejected. Running escrow means holding client
funds, which requires money transmitter licenses in most jurisdictions.

**USDC on Polygon.** Considered. The platform already uses Polygon.
Programmatic escrow, transparent settlement.

Rejected for v1: consumer UX in target markets (MY, ID, Gulf) is
card/wallet-based. Regulatory clarity varies. Shariah Board has not issued
stablecoin guidance. Hybrid (card primary, crypto optional) is future
consideration.

**Defer Payments entirely.** Rejected. Without payment, the marketplace
is a catalog.

## Dissent

**Platform Architecture** wanted a concrete vendor in ENT-001. *Overruled
with mitigation:* the port is validated against **two** candidate providers'
APIs during design, not zero. If neither fits, the port is wrong and we
iterate before committing.

**Finance** wanted the cheapest provider meeting mandatory criteria.
*Accepted in principle, deferred:* fee structure is one of eight criteria.
Cost is not the sole determinant for a system handling buyer funds.

**Shariah Advisory Board** asked how we build a payment system before
Board approval. *Answered:* the Board is engaged in procurement. Their
approval is a mandatory criterion (1, 4). No `Payments` code reaches
production without Board sign-off. The deferral is of vendor selection,
not of Shariah review.

## Reversibility

Vendor swap is moderate. New adapter, in-flight transaction migration
(complex if escrow is active), buyer/vendor notification, historical
reconciliation. ~4–8 engineer-weeks for code, 4–6 weeks operational
transition.

Port shape change is expensive. Every adapter reworked. This is why the
port is validated against multiple vendors before implementation.

## References

- `docs/ARCHITECTURE.md` §7.4
- `docs/procurement/payment-provider-evaluation.xlsx`
- `HalalChain.Application/Payments/IPaymentProvider.cs`
