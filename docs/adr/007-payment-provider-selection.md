# ADR-007 — Payment provider selection

**Status:** Accepted — vendor TBD pending procurement
**Date:** 2026-09-26
**Deciders:** Finance, Compliance
**Consulted:** Shariah Advisory Board, Legal, Platform Architecture
**Informed:** Steering Committee

## Context

The marketplace requires a payment provider. Most payment providers assume
a conventional fee model where the platform holds buyer funds and pays
vendors later, often with interest-bearing settlement accounts. This is not
compatible with the halal-finance constraints the platform operates under.

The constraints (from `docs/ARCHITECTURE.md` §7.4):

- **No riba.** No interest on delayed payouts. No interest-bearing escrow
  product. Platform revenue is a fixed service fee (ujrah) disclosed at
  listing time.
- **No gharar.** Delivery terms stated explicitly before capture. No
  open-ended "ships when ready."
- **Wakala-based escrow.** Where funds are held between capture and payout,
  the structure is agency (wakala), documented.
- **Zakat and sadaqah pass-through.** Buyer opt-in, separate line item,
  100% to the designated recipient, no commingling.

Very few payment providers support these constraints natively. Most can be
adapted with contractual addenda and escrow structures, but the adaptation
must be legally sound, not just technically feasible.

## Decision

**The Payments module is built against a port (`IPaymentProvider`), and
vendor selection proceeds in parallel.**

The port is defined now; the vendor is selected through a procurement
process that runs during ENT-002. The port shape is:

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

The port has no interest-related methods. It has no "hold funds and pay
later" concept that assumes a fee. The escrow semantics are structural:
the provider holds funds under a documented agency agreement, and the
platform's split allocates principal to vendor and service fee to the
platform separately.

### Procurement criteria

| # | Criterion | Weight | Mandatory |
|---|---|---|---|
| 1 | Supports wakala escrow structure or contracts to support it | 25% | Yes |
| 2 | No interest on delayed payouts, contractually | 20% | Yes |
| 3 | Split payment with per-vendor allocation | 15% | Yes |
| 4 | Zakat/sadaqah pass-through without commingling | 15% | Yes |
| 5 | SAQ-A eligible (no card data in platform scope) | 10% | Yes |
| 6 | Multi-currency support (MYR, IDR, AED, USD) | 5% | No |
| 7 | Fee structure transparency | 5% | No |
| 8 | API quality, SDK, sandbox | 5% | No |

Vendors scoring below "mandatory yes" on any of criteria 1–5 are
disqualified before weighting.

### Candidate landscape (as of September 2026)

| Vendor | Wakala capable | No riba | Splits | Zakat pass-through | SAQ-A |
|---|---|---|---|---|---|
| Provider A | Yes (documented) | Yes | Yes | Yes (dedicated ledger) | Yes |
| Provider B | Contract addendum | Yes | Yes | Limited | Yes |
| Provider C | No native | Yes | Yes | No | Yes |
| Provider D | Yes | Contract required | Yes | Yes | Yes |

Vendor names redacted in this ADR; full evaluation filed at
`docs/procurement/payment-provider-evaluation.xlsx`.

**Decision deferred to procurement.** Target: close by end of ENT-002
week 4, or Payments module (build order step 31) cannot proceed.

## Consequences

**Easier:**
- The Payments module can be built and tested against a stub provider
  before the vendor is selected
- Switching providers is a config change, not a rewrite
- The port shape forces the halal-finance constraints into the interface,
  not into provider-specific code
- Vendor selection can proceed in parallel with implementation

**Harder:**
- The stub provider must correctly model wakala escrow semantics, which
  requires legal review of what "wakala" means in the interface
- The port may not fit a chosen vendor's API cleanly; adapter code will
  be needed regardless
- Deferred decision means the Payments module has a real deadline; missing
  it delays ENT-002

## Alternatives considered

### Alternative A — Select vendor first, then design the port

Rejected. This inverts the dependency. The port should be shaped by the
domain constraints, not by a vendor's API. If we shaped the port around
Vendor A and then Vendor B was selected, we would be reworking the port.

### Alternative B — Build a bespoke escrow system

Rejected. Running our own escrow means holding client funds, which requires
money transmitter licenses in most jurisdictions. This is a multi-year
regulatory path. Not viable.

### Alternative C — Use a crypto payment rail (USDC on Polygon)

Considered. The platform already uses Polygon. USDC settlement on-chain
would be programmatically escrow-able and transparent.

Rejected for v1 because:
- Consumer payment UX in target markets (Malaysia, Indonesia, Gulf) is
  card- and wallet-based, not crypto-based
- Regulatory clarity for crypto payments varies by market
- The Shariah Board has not yet issued guidance on stablecoin-based escrow
- A hybrid approach (card primary, crypto optional) is a future
  consideration, not a v1 commitment

### Alternative D — Defer Payments entirely, build marketplace without it

Rejected. Without payment, the marketplace is a catalog. The whole
value proposition requires transacting.

## Dissent

**Platform Architecture** argued for a concrete vendor choice in ENT-001.
Their position: "Building against an unimplemented port is abstraction
without validation. We will discover the port is wrong when we try to
wire the actual provider."

**Overruled with mitigation.** The mitigation is that the port is validated
against **two** candidate providers' APIs during design, not zero. If both
fit the port with reasonable adapter effort, the port is validated. If
neither fits, the port is wrong and we iterate before committing to a
vendor. This is a weaker validation than a concrete implementation, but it
is not abstraction without evidence.

**Finance** argued for the cheapest provider meeting mandatory criteria.
Their position: "All the mandatory criteria are met by at least two
vendors. Choose the cheaper."

**Accepted in principle, deferred in practice.** Fee structure is one
criterion among eight. The final selection weighs all criteria. Cost is
not the sole determinant for a system that handles buyer funds.

**Shariah Advisory Board** raised a concern about the deferral itself.
Their position: "How can you build a payment system before the Shariah
Board has approved the structure?"

**Answered.** The Shariah Board is engaged in the procurement process.
Their approval is one of the mandatory criteria (criteria 1, 4). No
Payments code reaches production without Board sign-off. The deferral is
of vendor selection, not of Shariah review.

**One dissent not held:** an early proposal was to select a vendor based
on "best API" alone. This was rejected. API quality is 5% weight. The
constraints dominate.

## Reversibility

**Vendor swap is a moderate refactor.** Switching payment providers
requires:

1. New adapter implementing `IPaymentProvider`
2. Migration of in-flight transactions (complex if escrow is active)
3. Buyer and vendor notification for account/routing changes
4. Reconciliation of historical data

Estimated cost: 4–8 engineer-weeks for the code, 4–6 weeks of operational
transition for a live marketplace. Not something to do casually after
launch, but possible.

**Port shape change is expensive.** Every adapter must be updated. If the
port is wrong, we are wrong everywhere. This is why the port shape is
validated against multiple vendor APIs before implementation.

## References

- `docs/ARCHITECTURE.md` §7.4 — halal finance constraints
- `docs/procurement/payment-provider-evaluation.xlsx` — full evaluation
- Shariah Board memo on wakala escrow (pending)
- `HalalChain.Application/Payments/IPaymentProvider.cs`
