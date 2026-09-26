# ADR-006 — Chain network selection

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Platform Architecture, Finance
**Consulted:** Compliance, Security, Shariah Advisory Board
**Informed:** Steering Committee

## Context

The platform anchors Merkle roots of evidence batches on-chain. Three
production candidates were evaluated:

- **Polygon PoS** — mature, cheap, fast finality, high throughput
- **Polygon zkEVM** — privacy-preserving, slower finality, higher cost
- **Polygon CDK (custom zk chain)** — full control, higher operational cost

The anchor cadence is hourly. Anchoring is not latency-sensitive; verification
must be reasonably fast but can tolerate minutes. Privacy matters because
Merkle roots, while not revealing evidence content, reveal batch sizes and
timing.

## Decision

**Primary: Polygon zkEVM.** Failover: Polygon PoS.

The `IBlockchainClient` port abstracts the network. Configuration selects
the network; no code change is required to switch.

### Primary rationale (zkEVM)

- Zero-knowledge proofs provide privacy guarantees for batch composition.
  Observers can verify that a root was anchored without learning batch
  details.
- Validity proofs provide strong integrity guarantees — the state transition
  is mathematically verified, not merely attested by validators.
- Compliance auditors appreciate the cryptographic finality.
- The recent Type-1 equivalence upgrade reduced prover costs by ~73%.

### Failover rationale (PoS)

- If zkEVM finality times become operationally unacceptable (> 2 hours in
  practice), the failover to PoS is a config change.
- PoS finality is ~5 minutes, which is ample for our anchor cadence.
- PoS is cheaper, which matters if gas prices spike.

### Configuration

```json
{
  "Blockchain": {
    "Provider": "nethereum",
    "Network": "polygon-zkevm",
    "RpcUrl": "https://zkevm-rpc.com",
    "ChainId": 1101,
    "FailoverNetwork": "polygon-pos",
    "FailoverRpcUrl": "https://polygon-rpc.com",
    "ContractAddresses": {
      "EvidenceAnchor": "0x...",
      "CertificateRegistry": "0x...",
      "PolicyAnchor": "0x..."
    },
    "AnchorCadence": "01:00:00",
    "AnchorCadenceMax": "06:00:00",
    "BatchSize": 500
  }
}
```

`AnchorCadence` is a **target**, not a fixed interval. It is a ceiling
during gas spikes; batching more aggressively when gas is high is permitted.
The `AnchorCadenceMax` is the hard ceiling.

## Consequences

**Easier:**
- Privacy-preserving anchor verification with cryptographic guarantees
- Compliance auditors accept zkEVM proofs without additional explanation
- Failover to PoS is a config change, not a code change
- Contract deployment targets both networks with the same bytecode

**Harder:**
- zkEVM finality is ~30 minutes, significantly slower than PoS. Verification
  of a recent anchor may need to wait. Acceptable for our cadence.
- Gas costs are higher on zkEVM. The cost model targets account for this.
- Contract address management: two sets of deployed addresses, one per
  network. Config schema supports this.
- DevChain is used for local development; neither production network
  is used in dev.

## Alternatives considered

### Alternative A — Polygon PoS as primary

Rejected. PoS is mature and cheap, but its privacy properties are weaker.
Merkle roots and batch sizes are fully visible. For a compliance platform
where evidence patterns reveal vendor activity, this is a real concern.
PoS remains the failover.

### Alternative B — Polygon CDK (custom zk chain)

Rejected on operational grounds. Running our own zk chain means running our
own sequencer, prover, and validator set. This is a multi-person
infrastructure commitment. Not justified at current scale. Reconsider if
the platform reaches a scale where custom chains are economically rational
(e.g. > 1M anchors/day).

### Alternative C — Ethereum L1

Rejected on cost. L1 gas costs are prohibitive for hourly anchoring. Even
batched, the cost per anchor would exceed the batch's business value.

### Alternative D — No chain, just a signed audit log

Considered seriously. A signed append-only log (e.g. via a transparency
log) provides similar integrity guarantees without blockchain overhead.
Rejected because the platform's value proposition includes verifiable
anchoring that third parties can check without trusting the platform.
A signed log requires trusting the signer; a chain does not.

### Alternative E — Arweave or Filecoin for evidence storage

Rejected for this ADR. Those are storage networks, not anchor chains.
They are orthogonal; if we adopt them, it is a storage-layer decision (see
ADR-002), not a chain decision.

## Dissent

**Finance** argued for Polygon PoS primary. Their position: "zkEVM gas is
2–4× PoS. For an hourly anchor, that is a 4-figure monthly delta at our
volume. The privacy benefit is theoretical."

**Overruled because:** the privacy benefit is not theoretical for a
compliance platform. The batch composition of a halal certifier's
verification activity is sensitive — it reveals which vendors are being
audited and when. Competitors and short-sellers could extract signal from
public batch timing. zkEVM's privacy is a defensible requirement.

**Mitigation:** the failover to PoS is documented and configured. If gas
costs on zkEVM spike by more than 5× for more than 30 days, the ADR is
revisited.

**Security** argued that zkEVM's relative immaturity is a risk. Their
position: "Polygon PoS has been running for years; zkEVM is newer. Newer
means unaudited edge cases."

**Accepted as a risk, mitigated.** The blockchain module is downstream of
verdict decisions (P6). If the chain fails, evidence integrity is not
immediately compromised — the evidence is content-addressed and stored
off-chain. Anchoring resumes when the chain recovers. We are not dependent
on the chain for correctness, only for third-party verifiability.

**Shariah Advisory Board** asked whether anchoring on a blockchain with
any riba-adjacent features (staking rewards, etc.) is problematic. Their
position: "If we anchor on a network whose validator economics include
interest, are we complicit?"

**Answered and documented.** Polygon's validator rewards are fee-based and
do not constitute riba. The Shariah Board reviewed the validator economics
and issued a memorandum confirming the network's use is permissible for
evidence anchoring. The memorandum is filed at
`docs/shariah/polygon-anchor-memo.pdf`.

**One dissent held:** the anchor cadence is a ceiling, not a fixed interval.
This is captured in the config schema. The dissent was that fixed cadence
is simpler to reason about. **Overruled because:** during gas spikes,
a fixed cadence either overpays or delays. Adaptive cadence within a
ceiling is the pragmatic choice.

## Reversibility

**Chain swap is a config change.** The `IBlockchainClient` port abstracts
the network. Switching from zkEVM to PoS:

1. Deploy contracts to PoS (bytecode is identical)
2. Update `Network`, `RpcUrl`, `ChainId`, and contract addresses in config
3. Restart the anchor service

Existing anchors on zkEVM remain verifiable. New anchors go to PoS. No
migration of existing anchors is required; they live on their original
chain.

**Multi-chain support is possible** but not implemented. If we need to
anchor on both networks for redundancy, the `IBlockchainClient` port
supports multiple implementations behind a facade. Estimated cost: 2
engineer-weeks.

## References

- ADR-002 — Storage port/adapter (Merkle tree shape)
- `docs/shariah/polygon-anchor-memo.pdf` — Shariah Board memorandum
- `HalalChain.Platform.Api/Modules/Blockchain/Contracts/IBlockchainClient.cs`
- `service-manifest.yaml` — deployed contract addresses
