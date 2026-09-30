# Falsification Report — ENT-001

**Status:** TEMPLATE — to be completed by an independent reviewer
**Reviewer:** _________________________ (must not be an author of any ADR)
**Review date:** ______________________
**Target:** `docs/ARCHITECTURE.md` + ADR-001 … ADR-009
**Principles under test:** `.cline_inbox/PRINCIPLES.md` P1–P7

## Purpose

ENT-001's gate (`gate-001-002`) requires the architecture to survive an
adversarial review before ratification. This report is that review.

The reviewer attempts to falsify each of the seven enforcement layers
(P1–P7). A layer is **held** if the attacker fails. A layer is **falsified**
if a concrete attack succeeds or if the layer's enforcement is weaker than
claimed. A falsified layer blocks ratification.

> A layer that was not tested has not passed. "Held" is a claim about an
> attack you actually ran, not a restatement of the ADR.

## Rules

1. The reviewer must not be an author of any ADR being reviewed.
2. Each attack must be concrete — a specific code path, config, or
   operational sequence, not a category.
3. "Held" requires evidence of the enforcement mechanism working. Not
   "the doc says it works."
4. If falsified, the required fix is documented, and the review re-runs
   after the fix.
5. The reviewer signs the report. Their signature is the evidence.

## Enforcement inventory (verify before attacking)

These are the artifacts each layer claims rests on. Confirm they still exist
and still run before recording a result — an enforcement mechanism that is
present but never executed does not hold a layer.

| Layer | Claimed enforcement | Exists? |
|---|---|---|
| P1 | `HalalChain.Application/Agentic/Models/EvidenceProposal.cs`; `HalalChain.Agents.Tests/VerdictBoundaryTests.cs`; `.cline_inbox/hooks/principles.mjs` | verify |
| P2 | IAM bucket policy; `BLOB__S3__ACCESS_KEY` vs `BLOB__S3__READONLY_KEY`; config-lint in CI | verify |
| P3 | `HalalChain.Application/Storage/IBlobStore.cs` (no `Delete`); `HalalChain.Storage/Evidence/RetentionEvaluator.cs` | verify |
| P4 | `HalalChain.Architecture.Tests/DependencyRulesTests.cs`; ADR-005 | verify |
| P5 | `DependencyRulesTests.Storage_AssemblyDeclaresNoMerkleTreeType`; ADR-002 merge decision M1 | verify |
| P6 | `contracts/src/*.sol` token scan; `HalalAccessControl.sol` + 4 registries | verify |
| P7 | ADR-008; deployment checklist sign-off | verify |

---

## P1 — No verdict field on agent output

**Claim:** `EvidenceProposal` and its derivatives contain no field named
`verdict`, `decision`, `is_halal`, `status`, `approved`, or `rejected`. CI
fails the build on any match.

**Enforcement:** `EvidenceProposal` is a C# `sealed record` with five members
(`Kind`, `Summary`, `Confidence`, `SourceRefs`, `ProposedEvidence`).
`VerdictBoundaryTests` reflects over the `HalalChain.Agents.Eval` namespace
and fails on any member whose name contains `verdict`, `compliance`,
`halalstatus`, `decision`, `outcome`, `approved`, or `rejected`, and on any
member *typed* as `VerdictState` or `VerdictBinding`.

**Attack:** Add a `verdict: Literal["halal", "haram"]` field to the agent
output type in `.halalchain/agents/app/`. Commit. Observe CI.

**Expected:** The meta-test step fails; the commit is rejected.

**Note for the reviewer:** the .NET guard reflects over the *`HalalChain.Agents.Eval`
namespace*, which is the eval harness. The Python agent runtime under
`.halalchain/agents/app/` is covered by `.cline_inbox/hooks/principles.mjs`,
not by the C# reflection test. Confirm which of the two actually covers the
file you edit, and confirm that `principles.mjs` is invoked by CI at all.

**Result:** ☐ Held  ☐ Falsified

**Evidence:** _____________________________________________

---

## P2 — Only tawheed holds blob write credentials

**Claim:** `ai-inference` and `agents` hold credentials with `GetObject`
only. Neither can perform `PutObject` or `DeleteObject`.

**Attack:** Attempt to write a file to the evidence bucket from a shell
inside the `agents` container using the credentials present in its
environment.

**Expected:** S3/MinIO returns `AccessDenied`. The bucket policy or IAM
role denies the operation.

**Result:** ☐ Held  ☐ Falsified

**Evidence:** _____________________________________________

---

## P3 — Evidence is append-only

**Claim:** `IBlobStore` declares no `Delete` member. Retention tombstones
metadata; it never purges bytes as a side effect.

**Enforcement:** `IBlobStore` exposes exactly `PutAsync`, `OpenReadAsync`,
and `ExistsAsync`. `DependencyRulesTests` reflects over the interface and
fails on any member named `Delete*` or `Remove*`.

**Attack:** Search the codebase for any method that deletes a blob. Check
the `IBlobStore` interface, all implementations, and any reflection-based
invocation. Also audit `EvidenceStore` and `RetentionEvaluator` to confirm
retention does not reach a delete operation.

**Expected:** No `Delete`/`Remove` member on `IBlobStore`. No implementation
exposes one. `RetentionEvaluator` produces Keep / Extend / Tombstone
decisions that update metadata only.

**Result:** ☐ Held  ☐ Falsified

**Evidence:** _____________________________________________

---

## P4 — Only Compliance mutates ProductStatus

**Claim:** `ProductStatus` is mutated only within
`HalalChain.Platform.Api/Modules/Compliance/`. All other modules read it.

**Attack:** Search the entire solution for assignments to `Product.Status`
or any setter on `ProductStatus`. Check for reflection, dynamic
invocation, or EF Core configuration that would allow bypass.

**Known weak point to attack first:** `Product.Status` is declared as
`public ProductStatus Status { get; set; }` — a public setter, so nothing
in the type system prevents any module from writing it. Also
`Product.HalalProfile` is a public settable `HalalProfile?` whose `Status`
is a `HalalStatus` enum. Establish whether that is a vendor-writable
halal-claim path, and whether a guard test actually fails on it. ADR-005
names `Rules/ModuleBoundaryRules.cs` as the enforcement point — confirm
whether that file exists or whether the rule is recorded in
`DependencyRulesTests.cs` instead.

**Expected:** A guard test fails on any write outside the Compliance module.
Manual grep confirms zero such writes.

**Result:** ☐ Held  ☐ Falsified

**Evidence:** _____________________________________________

---

## P5 — Two hashers, two concerns

**Claim:** SHA-256 is used only for `BlobRef` content addressing.
Keccak256 is used for on-chain Merkle tree nodes. No SHA-256 Merkle tree
exists.

**Enforcement:** `DependencyRulesTests.Storage_AssemblyDeclaresNoMerkleTreeType`
fails if *any* type in `HalalChain.Storage` has "Merkle" in its name.

**Attack:** (a) Search the solution for a `Sha256MerkleTree`. (b) Verify
the Merkle tree implementation uses keccak256 for internal nodes and
sorted-pair combination. (c) Establish whether a C#↔Solidity root-parity
test actually exists — if there is no such test, say so; do not assume one
runs. (d) Confirm Foundry is invoked in CI.

**Expected:** No SHA-256 Merkle tree. Parity test green where one exists.

**Result:** ☐ Held  ☐ Falsified

**Evidence:** _____________________________________________

---

## P6 — No verdict type on-chain

**Claim:** No Solidity contract declares a type, function, event, or
variable named `verdict`, `halal`, `approved`, `halalStatus`, or
`isHalal`.

**Attack:** Run the CI grep against
`HalalChain.Platform.Contracts/contracts/src/`. Add a function to
`HalalCertificationRegistry.sol` that takes a boolean halal flag. Commit.
Observe CI.

**Expected:** CI fails. The contract's purpose is existence, revocation,
and expiry — three deterministic facts. It cannot represent a verdict.

**Note for the reviewer:** confirm the grep covers `contracts/src/` only and
does not silently match vendored libraries under `contracts/lib/`, where
OpenZeppelin's own contracts legitimately contain such vocabulary. A grep
that passes because it scanned nothing is not a held layer.

**Result:** ☐ Held  ☐ Falsified

**Evidence:** _____________________________________________

---

## P7 — Registrar key never in a hot wallet in production

**Claim:** Production registrar key is KMS-backed. The `tawheed` service
is the only holder of sign permission. The private key material never
leaves the KMS boundary.

**Attack:** (a) Inspect `tawheed`'s environment and configuration; confirm
no private key is present. (b) Attempt to sign a transaction from a
non-`tawheed` service using IAM credentials; confirm denial. (c) Review
CloudTrail (or equivalent) for sign operations; confirm they originate only
from `tawheed`. (d) Confirm the multi-signature governance flow for
registrar rotation.

**Expected:** No key material in any process. Only `tawheed` has sign
permission. Rotation requires 3-of-3 signatures.

**Result:** ☐ Held  ☐ Falsified

**Evidence:** _____________________________________________

---

## Additional attacks (reviewer's discretion)

The reviewer may add attacks. Suggested directions:

- **Principle P8** — attempt to start a workflow without a budget
- **Principle P9** — attempt to cache the checkout compliance check
- **Principle P10** — attempt to produce a verdict without a trace hash
- **Cross-cutting** — attempt to bypass the pre-commit hook
  (`git commit --no-verify`) and confirm CI still catches the violation

## Summary

| Layer | Result |
|---|---|
| P1 — agent output | ☐ Held ☐ Falsified |
| P2 — blob write creds | ☐ Held ☐ Falsified |
| P3 — append-only | ☐ Held ☐ Falsified |
| P4 — Compliance-only mutation | ☐ Held ☐ Falsified |
| P5 — two hashers | ☐ Held ☐ Falsified |
| P6 — no verdict on-chain | ☐ Held ☐ Falsified |
| P7 — KMS registrar key | ☐ Held ☐ Falsified |

**Gate `gate-001-002` cannot pass until all seven are Held.**

## Falsified layers — remediation

For each falsified layer:

| Layer | Attack | Required fix | Owner | Re-test date |
|---|---|---|---|---|
| | | | | |

## Reviewer attestation

I confirm that:

1. I am not an author of any ADR under review.
2. I performed the attacks described above.
3. The results reflect what actually happened, not what was expected.
4. Where a layer is marked Held, I attempted to falsify it and failed.

**Signature:** _____________________  **Date:** ____________

## Steering Committee ratification

The architecture is ratified only after this report shows all seven layers
Held.

**Steering Chair:** _____________________  **Date:** ____________
