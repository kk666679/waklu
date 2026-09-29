# ENT-002 — Enterprise Implementation

**Role:** Build
**Owner:** Platform Engineering
**Approver:** Platform Architecture
**Duration:** 20 weeks
**Exit:** Gate `gate-002-003`

## Objective

Implement the platform to spec, in the build order defined below.
**Constraints land before the code they constrain.** No exceptions.

## Mandatory constraints

- **P1–P10** apply to every commit. The pre-commit hook enforces P1, P5, P6
  locally. CI enforces the rest.
- **Build order is not a suggestion.** Out-of-order commits fail CI.
- **Every step has a prerequisite and a gate.** Do not start step N+1 until
  step N's gate passes.
- **Architecture tests exist before the features they guard.**

## Mandatory build order

| # | Step | Prerequisite | Gate to proceed |
|---|---|---|---|
| 1 | Storage ports (`IBlobStore`, `IEvidenceStore`) in `Application/Storage/` | ADRs ratified | Interfaces compile; no `Delete` on `IBlobStore` (P3) |
| 2 | `FileSystemBlobStore` + `Sha256ContentHasher` | Step 1 | Contract tests pass; coverage ≥ 80% |
| 3 | Storage arch tests (P3, P5 negative cases) | Step 2 | All arch rules green |
| 4 | Agent runtime: protocol, `EvidenceProposal`, trace, budget, escalation | Step 3 | Types compile; no verdict field (P1) |
| 5 | CI meta-test grepping for forbidden verdict fields | Step 4 | Meta-test green; would fail on P1 violation |
| 6 | `explainer` agent (safest canary; runs post-decision) | Step 5 | Runs after `tawheed`; cannot influence a verdict |
| 7 | `Vendors` + `Storefront` modules | Step 6 | Vendor onboarding end-to-end with fake `tawheed` |
| 8 | `Compliance` module: state machine, `VerdictBinding`, status projection | Step 7 | Write-path arch test: only `Compliance` mutates `ProductStatus` (P4) |
| 9 | `Catalog`: products, variants, media, certificates | Step 8 | Listing blocked without a valid `VerdictBinding` |
| 10 | Certificate expiry sweep (hosted service) | Step 9 | Deterministic; produces status transitions; runs daily |
| 11 | Solidity contract interfaces + custom errors | ADR-006 | `forge build --sizes` under ceiling |
| 12 | `MerkleHelper.sol` + parity test vs OpenZeppelin | Step 11 | Parity green in CI |
| 13 | Contract implementations (`EvidenceAnchor`, `CertificateRegistry`, `PolicyAnchor`) | Step 12 | `forge build`; gas under baselines |
| 14 | Contract unit tests | Step 13 | All pass |
| 15 | Contract fuzz tests | Step 14 | 10,000 runs clean in CI |
| 16 | Contract invariant tests (handlers, bounded) | Step 15 | 512 runs, depth 64, clean |
| 17 | Contract integration test (full lifecycle) | Step 16 | Register → anchor → verify → policy → expiry |
| 18 | `.gas-snapshot` committed; CI gate active | Step 17 | Regression fails build |
| 19 | Slither clean | Step 18 | Zero high, zero medium |
| 20 | `KeccakMerkleTree` C# implementation + parity against Solidity | Step 12 | Matches `MerkleHelper` output byte-for-byte |
| 21 | `IBlockchainClient` port + `InMemoryBlockchainClient` | Step 20 | No Nethereum yet; port is testable |
| 22 | Deploy contracts to Polygon Amoy | Step 19 | Verified on explorer; addresses in manifest |
| 23 | Nethereum adapter against DevChain | Step 22 | Aspire orchestration; DevChain in local stack |
| 24 | Anchor cadence hosted service | Step 23 | Batches hourly; adaptive cadence ceiling documented |
| 25 | Verification endpoint + inclusion proof | Step 24 | End-to-end verification returns `VerificationResult` |
| 26 | Blockchain arch tests (P5, P6, no verdict type) | Step 25 | All rules green |
| 27 | `Search` + `Cart` with checkout re-verification | Step 26 | Not cacheable (P9); integration test proves it |
| 28 | `Orders` + `Fulfillment` — sub-order split | Step 27 | One capture, per-vendor payout |
| 29 | `classifier` + `verifier` agents | Step 28 | Bounded scope; proposals only |
| 30 | `S3BlobStore` + MinIO under `storage` profile | Step 29 | `service-manifest.yaml` updated |
| 31 | `Payments` module | ADR-007 + Step 30 | Wakala escrow; no riba; pass-through zakat |
| 32 | `collector`, `profiler`, `gap` agents | Step 31 | External systems touched; budget bounds enforced |
| 33 | `workflows/supplier_onboarding` end-to-end | Step 32 | First full workflow completes within budget |
| 34 | `Reviews`, `Promotions`, `Disputes` | Step 33 | Accretive, non-blocking |
| 35 | Documentation pass (`AGENTS.md`, `README.md`, `docs/ARCHITECTURE.md`) | Step 34 | All three consistent; gates.yaml aligned |

Steps 3, 5, 18, and 26 are **constraint-before-code gates**. If a feature
ships before its guard test, the build fails.

## Deliverables

Full platform per `docs/ARCHITECTURE.md`, plus:

| # | Deliverable | Location |
|---|---|---|
| D1 | All storage adapters | `HalalChain.Storage/Adapters/` |
| D2 | Agent runtime | `.halalchain/agents/app/runtime/` |
| D3 | All six agents | `.halalchain/agents/app/agents/` |
| D4 | All four workflows | `.halalchain/agents/app/workflows/` |
| D5 | All Solidity contracts | `HalalChain.Platform.Contracts/contracts/src/` |
| D6 | Foundry test suite | `HalalChain.Platform.Contracts/contracts/test/` |
| D7 | `KeccakMerkleTree` (only Merkle impl) | `HalalChain.Platform.Api/Modules/Blockchain/Infrastructure/Merkle/` |
| D8 | Deployed contract addresses | `service-manifest.yaml` |
| D9 | All 12 marketplace modules | `HalalChain.Platform.Api/Modules/` |
| D10 | All arch tests | `HalalChain.Architecture.Tests/Rules/` |

## Continuous quality gates

Every commit must pass. A failing gate blocks merge.

| Gate | Threshold | Enforced by |
|---|---|---|
| Unit test coverage (Domain, Application) | ≥ 80% line | CI |
| Unit test coverage (Storage) | ≥ 90% line | CI |
| Architecture tests | 100% pass | CI |
| Foundry fuzz runs | 10,000 | CI |
| Foundry invariant runs | 512, depth 64 | CI |
| Gas regression | ≤ baseline | `forge snapshot --check` |
| Slither | zero high/medium | CI |
| Forbidden verdict fields | zero matches | CI grep |
| `Sha256MerkleTree.cs` existence | must not exist | CI presence check |
| Verdict type in Solidity | zero matches | CI grep |
| Docker image tags | no `latest`, no bare-major | CI lint |

## Exit gate criteria

| # | Criterion | Evidence | Verifier |
|---|---|---|---|
| G2.1 | All 35 build order steps complete | Commit log + ticket status | Platform Arch |
| G2.2 | All continuous quality gates green for 14 consecutive days | CI dashboard export | Platform Eng |
| G2.3 | Contract addresses in `service-manifest.yaml` | File inspection | Platform Ops |
| G2.4 | End-to-end supplier onboarding passes | Integration test log | Platform Eng |
| G2.5 | All six agents deployed, running under budgets | Runtime metrics 30d | Platform Eng |
| G2.6 | No open P1–P10 violations | Arch test dashboard | Arch Review Board |
| G2.7 | Documentation pass complete; three docs consistent | Manual review | Platform Arch |

## Risks

| Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|
| Build order violated under schedule pressure | High | Critical | CI enforces order; no manual override | Platform Eng Lead |
| Merkle parity test flaky | Medium | Critical | Pin forge-std version; cache dependencies | Platform Eng |
| Registrar key on Amoy compromised | Low | High | Amoy is testnet only; rotate on evidence of abuse | Security |
| Agent budget exhaustion at low rate | Medium | Medium | Monitor budget breach rate; tune bounds per workflow | Platform Eng |
| Contract gas regression | Medium | Medium | Snapshot check in CI; PR rejected on regression | Platform Eng |
| MAF 1.0-rc1 API churn | High | Medium | Pin exact version; isolate agent code behind interface | Platform Arch |

## RACI

| Activity | Platform Eng | Platform Arch | Security | Compliance | SRE |
|---|---|---|---|---|---|
| Storage layer | R | A | I | I | I |
| Agent runtime | R | A | C | C | I |
| Marketplace modules | R | A | I | C | I |
| Solidity contracts | R | A | C | C | I |
| Arch tests | R | A | I | I | I |
| Quality gates | R | C | I | I | A |
| Documentation | R | A | I | C | C |

## Out of scope

- Production deployment (ENT-004)
- Certification evidence collection (ENT-003)
- Multi-region deployment (ENT-005)

## Handoff to ENT-003

At phase close, Security & Compliance receives:

- Full platform in production-ready state (staging deployed)
- All quality gate dashboards
- Complete architecture test suite
- Contract ABI hash record
- All agent definitions and workflow budgets
- Documentation pass artifacts

Verification:

```bash
.cline_inbox/hooks/gate-check.sh gate-002-003
```
