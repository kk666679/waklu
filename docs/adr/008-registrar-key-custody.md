# ADR-008 — Registrar key custody

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Security, Platform Architecture
**Consulted:** Compliance, Finance, Legal
**Informed:** Steering Committee, CISO

## Context

The `CertificateRegistry` contract has an `onlyRegistrar` modifier. Only
the holder of the registrar key can register or revoke a certificate hash
on-chain. This is a high-value key:

- If the registrar key is compromised, an attacker can register arbitrary
  certificate hashes. The platform will treat them as current.
- If the registrar key is lost, certificate registration halts. Existing
  certificates remain valid but new ones cannot be added.
- The registrar key is not a "hot wallet" in the traditional sense; it
  does not hold funds. But its authority is significant.

Early discussion assumed a hot wallet on the application server, since
that is the simplest pattern. This is now rejected for production.

## Decision

**Production registrar key is KMS-backed. Hot wallets are permitted only
on the Amoy testnet.**

Specifically:

- **Amoy testnet**: hot wallet. Development convenience. No real value.
- **Polygon zkEVM and PoS production**: AWS KMS (or equivalent) with an
  asymmetric signing key. The private key never leaves the KMS boundary.
- The `tawheed` service is the only component with KMS sign permission.
  The `platform-api` service has no access to the registrar key.
- Key rotation: every 12 months, or immediately upon any suspicion of
  compromise. Rotation is a KMS operation; the on-chain registrar address
  is updated via a governance transaction that requires multi-signature
  approval.

### Signing flow

```
1. tawheed receives a certificate registration request
2. tawheed validates the request (evidence, vendor, expiry)
3. tawheed constructs the transaction (to CertificateRegistry, data: registerCertificate(...))
4. tawheed computes the keccak256 hash of the transaction
5. tawheed calls KMS.Sign(hash) → returns signature
6. tawheed submits the signed transaction to the Polygon RPC
7. On confirmation, tawheed records the anchor receipt
```

The private key material is never available to any process in the platform.
A memory dump of `tawheed`'s process space does not reveal the key.

### Multi-signature governance

Changing the registrar address requires:

1. Proposal by `tawheed` operator (signed KMS transaction)
2. Approval by CISO (hardware wallet signature)
3. Approval by Steering (hardware wallet signature)
4. On-chain execution of the `transferRegistrar` function

Three-of-three required. No single actor can change the registrar.

## Consequences

**Easier:**
- Key compromise requires compromising AWS KMS, not a process memory
- Key rotation is a KMS operation with audit trail
- Access to signing is logged and revocable via IAM
- Regulatory audits accept KMS custody as a control
- The `tawheed` service has the smallest possible attack surface for the
  highest-value operation

**Harder:**
- KMS adds latency (~100ms per signature) and cost (~$1 per 10k requests)
- The signing flow cannot be tested in unit tests with a real KMS; a
  local signing stub is used in dev
- Adding a new signer requires IAM changes, not just code
- The multi-signature governance step adds a day or two to registrar
  rotation

## Alternatives considered

### Alternative A — Hot wallet on the application server

Rejected for production. The private key lives in process memory or an
encrypted file. A memory dump, a debugger, a log with accidental
inclusion, or a supply chain compromise of a dependency all leak the key.
For a key that authorizes certificates, this is unacceptable.

### Alternative B — Hardware Security Module (HSM) on-premise

Considered. An on-prem HSM provides equivalent security to KMS with lower
per-signature latency. Rejected for v1 on operational grounds: HSMs require
physical security, dedicated infrastructure, and an operational team
trained on the specific HSM. KMS outsources this to the cloud provider.
Reconsider if signing volume exceeds 100k/month.

### Alternative C — Multi-signature wallet with distributed keys

Considered. A Gnosis Safe or similar multi-sig with keys held by 3
different people. This distributes trust but complicates automation —
every certificate registration requires 3 people to sign, which does not
scale.

Rejected for routine registrations. However, we adopt a variant of this
for **registrar address changes**: multi-signature governance is required
for that specific operation.

### Alternative D — Threshold signature scheme (TSS)

Considered. TSS provides distributed key management with a single
signature. Rejected for v1 as premature. KMS-backed single-signer is
sufficient. TSS can be adopted later if the threat model changes.

### Alternative E — No on-chain registrar, use a platform-signed message

Rejected. The whole point of on-chain registration is third-party
verifiability. A platform-signed message is only as good as the platform's
word.

## Dissent

**Platform Architecture** argued for a hot wallet with the key in AWS
Secrets Manager. Their position: "KMS adds latency and cost. Secrets
Manager is already in use. The key is encrypted at rest."

**Overruled because:** "encrypted at rest" is insufficient. The key must
be decrypted to be used, which means it lives in process memory during
signing. A memory dump at the right moment reveals it. KMS never exposes
the key. The latency and cost differences are acceptable given the
authority the key grants.

**Finance** raised the cost concern explicitly. Their position: "KMS
asymmetric signing costs real money at scale."

**Accepted and quantified.** At current projected volume (approximately
500 certificate registrations/month, plus 1 registrar rotation/year),
the KMS cost is negligible (< $50/year). If volume scales 100×, the cost
scales but remains small relative to the value of the certificates being
registered. The cost is not a blocker.

**Compliance** asked whether KMS custody satisfies audit requirements.
Their position: "Will external auditors accept KMS as a control?"

**Answered with precedent.** SOC 2, ISO 27001, and PCI DSS auditors
routinely accept cloud KMS as a key custody control. AWS KMS is FedRAMP
High and ISO 27001 certified. The audit trail (CloudTrail) logs every
sign operation, which is the evidence auditors require.

**One dissent held:** the multi-signature requirement for registrar
changes is non-negotiable. The dissent was that it slows incident
response — "if we suspect compromise, we need to change the registrar
now, not in 2 days."

**Overruled with an incident path.** A break-glass procedure exists:
under a declared security incident, the CISO can initiate an emergency
registrar change with a single hardware wallet signature, and the
post-incident review retroactively documents the action. The normal
3-of-3 procedure applies to planned rotations.

## Reversibility

**Cheap.** If KMS proves operationally painful, migration to an HSM or
TSS is a signing-layer change. The port (`IAnchorSigner`) abstracts the
signer. Estimated cost: 2 engineer-weeks.

**Registrar address change is a governance operation**, not a code change.
The address is stored in the contract; changing it requires the
multi-signature flow.

## References

- ADR-006 — Chain network selection (zkEVM and PoS both require registrar)
- `.cline_inbox/PRINCIPLES.md` — P7 (registrar key never in hot wallet in prod)
- `docs/security/kms-setup.md` — KMS configuration and IAM policy
- `HalalChain.Platform.Api/Modules/Blockchain/Infrastructure/Nethereum/AnchorSigner.cs`
