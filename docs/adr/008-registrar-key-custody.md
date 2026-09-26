# ADR-008 — Registrar key custody

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Security, Platform Architecture
**Consulted:** Compliance, Finance, Legal

## Context

`CertificateRegistry` has an `onlyRegistrar` modifier. Only the registrar
key holder can register or revoke certificate hashes on-chain.

- Compromise → attacker registers arbitrary certificates; platform treats
  them as current.
- Loss → certificate registration halts.
- Not a hot wallet in the traditional sense (holds no funds), but its
  authority is significant.

Early discussion assumed a hot wallet on the application server. Rejected
for production.

## Decision

**Production registrar key is KMS-backed. Hot wallet only on Amoy testnet.**

- **Amoy testnet:** hot wallet. Convenience. No real value.
- **Polygon zkEVM and PoS production:** AWS KMS (or equivalent) with
  asymmetric signing. Private key never leaves the KMS boundary.
- **`tawheed` is the only service with KMS sign permission.**
  `platform-api` has no registrar key access.
- **Rotation:** every 12 months, or immediately on suspected compromise.
  Registrar address change requires multi-signature governance.

### Signing flow

```
1. tawheed receives certificate registration request
2. tawheed validates (evidence, vendor, expiry)
3. tawheed constructs transaction (to CertificateRegistry, data: registerCertificate(...))
4. tawheed computes keccak256 of transaction
5. tawheed calls KMS.Sign(hash) → signature
6. tawheed submits signed transaction to Polygon RPC
7. On confirmation, tawheed records the anchor receipt
```

No process holds key material. A memory dump of `tawheed` reveals nothing.

### Multi-signature governance for registrar rotation

Three-of-three:
1. Proposal by `tawheed` operator (KMS-signed transaction)
2. Approval by CISO (hardware wallet)
3. Approval by Steering (hardware wallet)
4. On-chain `transferRegistrar`

**Break-glass:** under a declared security incident, CISO can initiate
emergency rotation with a single hardware wallet signature, documented
retroactively.

## Consequences

**Easier:** Key compromise requires compromising KMS, not a process.
Rotation is a KMS operation with audit trail. Access is logged and IAM-
revocable. Regulatory audits accept KMS as a control. Minimal attack
surface for the highest-value operation.

**Harder:** KMS adds ~100ms latency and small per-signature cost. Unit
tests use a local stub. Adding a signer requires IAM changes. Rotation is
a day or two of coordination.

## Alternatives considered

**Hot wallet on app server.** Rejected. Private key lives in process
memory. A memory dump, debugger, accidental log, or supply-chain compromise
leaks it.

**On-prem HSM.** Considered. Equivalent security, lower latency. Rejected
on operations: physical security, dedicated infra, trained staff. Reconsider
at >100k signings/month.

**Multi-signature wallet for routine registrations.** Rejected. 3 people
per certificate does not scale. Adopted as the **rotation** mechanism
only.

**Threshold signature scheme (TSS).** Considered. Distributed key
management with single signature. Rejected for v1 as premature. Adopt if
threat model changes.

**Platform-signed message, no on-chain registrar.** Rejected. The point
of on-chain registration is third-party verifiability. A platform signature
is only as good as the platform's word.

## Dissent

**Platform Architecture** wanted hot wallet with AWS Secrets Manager.
*Overruled:* "encrypted at rest" is insufficient. The key must be
decrypted to be used, so it lives in process memory during signing. KMS
never exposes it. Latency and cost differences are acceptable.

**Finance** raised cost. *Accepted and quantified:* at ~500 registrations/
month, KMS cost < $50/year. Even at 100×, small relative to value of
certificates registered.

**Compliance** asked whether KMS satisfies auditors. *Answered with
precedent:* SOC 2, ISO 27001, PCI DSS auditors routinely accept cloud KMS.
AWS KMS is FedRAMP High and ISO 27001 certified. CloudTrail logs every sign
operation — the evidence auditors require.

**One rule held:** multi-signature rotation is non-negotiable. *Overruled
the dissent* that it slows incident response — the break-glass path exists.

## Reversibility

Cheap. Signing layer change. `IAnchorSigner` port abstracts. ~2
engineer-weeks. Registrar address change is governance, not code.

## References

- ADR-006 — Chain selection
- `.cline_inbox/PRINCIPLES.md` — P7
- `docs/security/kms-setup.md`
