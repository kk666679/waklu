# ADR-006 — Chain network selection

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Platform Architecture, Finance
**Consulted:** Compliance, Security, Shariah Advisory Board

## Context

The platform anchors Merkle roots of evidence batches on-chain. Anchor
cadence is hourly. Anchoring is not latency-sensitive; verification must
be reasonably fast but tolerates minutes. Privacy matters — Merkle roots
reveal batch timing and size, which correlate with audit activity.

## Decision

**Primary: Polygon zkEVM. Failover: Polygon PoS.**

`IBlockchainClient` abstracts the network. Config selects; no code change
required to switch.

```json
{
  "Blockchain": {
    "Provider": "nethereum",
    "Network": "polygon-zkevm",
    "RpcUrl": "https://zkevm-rpc.com",
    "ChainId": 1101,
    "FailoverNetwork": "polygon-pos",
    "FailoverRpcUrl": "https://polygon-rpc.com",
    "AnchorCadence": "01:00:00",
    "AnchorCadenceMax": "06:00:00",
    "BatchSize": 500
  }
}
```

`AnchorCadence` is a **target**, not a fixed interval. Adaptive within the
ceiling during gas spikes. `AnchorCadenceMax` is the hard limit.

**zkEVM rationale:** privacy via zero-knowledge proofs, cryptographic
finality, ~73% prover-cost reduction from the Type-1 upgrade.

**PoS failover rationale:** ~5 minute finality (vs zkEVM's ~30 minutes),
cheaper. Failover is a config change.

## Consequences

**Easier:** Privacy-preserving anchors. Compliance auditors accept zkEVM
proofs. Failover is config. Same bytecode on both networks.

**Harder:** zkEVM finality is slow. Higher gas. Two sets of deployed
addresses.

## Alternatives considered

**Polygon PoS primary.** Rejected. Weaker privacy — batch composition is
fully visible. Remains failover.

**Polygon CDK (custom zk chain).** Rejected on operations. Running a
sequencer and prover is a multi-person commitment. Reconsider at >1M
anchors/day.

**Ethereum L1.** Rejected on cost.

**Signed transparency log, no chain.** Considered seriously. Similar
integrity without blockchain overhead. Rejected: the value proposition is
verifiability by third parties *without trusting the platform*. A signed
log requires trusting the signer.

**Arweave/Filecoin for evidence.** Orthogonal — those are storage
networks, not anchor chains. A storage-layer decision (ADR-002), not here.

## Dissent

**Finance** wanted PoS primary. *Overruled:* batch composition of a halal
certifier's activity is sensitive — it reveals which vendors are being
audited and when. zkEVM's privacy is a defensible requirement. If zkEVM
gas spikes by >5× for >30 days, this ADR is revisited.

**Security** flagged zkEVM's relative immaturity. *Accepted as a risk,
mitigated:* the blockchain module is downstream of verdict decisions (P6).
If the chain fails, evidence integrity is unaffected — evidence is
content-addressed off-chain. Anchoring resumes when the chain recovers.

**Shariah Advisory Board** asked whether anchoring on a network with
riba-adjacent validator economics is problematic. *Answered:* Polygon's
validator rewards are fee-based, not interest. The Board reviewed and
issued a memorandum filed at `docs/shariah/polygon-anchor-memo.pdf`.

**One rule held:** anchor cadence is a ceiling, not a fixed interval.
*Overruled the dissent* that fixed is simpler: during gas spikes, a fixed
cadence either overpays or delays. Adaptive within a ceiling is pragmatic.

## Reversibility

Chain swap is a config change. Deploy contracts to new network, update
config, restart. Existing anchors remain on their original chain. No
migration required.

Multi-chain support is possible but not implemented.

## References

- ADR-002 — Merkle tree shape
- `docs/shariah/polygon-anchor-memo.pdf`
- `HalalChain.Platform.Api/Modules/Blockchain/Contracts/IBlockchainClient.cs`
