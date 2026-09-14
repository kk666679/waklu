# Threat Model — Blockchain, Smart-Contract & IPFS layer

| Threat | Impact | Likelihood | Mitigation | Detection | Recovery |
|---|---|---|---|---|---|
| **Fake certification** (attacker calls `issueCertificate` on a product) | Critical — the entire trust signal | Med | Only `CERTIFIER_ROLE` can issue; certifier wallets are KYC'd; timelock-onboarded (24h on mainnet); cert body is hashed on-chain (`metadataHash`); the cert body itself is on IPFS and re-fetched on `/verify` | Indexer flags certs that reference a certifier whose role was revoked; the public QR page shows the certifier and jurisdiction, so consumers can cross-check | `revokeCertificate`; re-issue flow; certifier wallet added to denylist |
| **Compromised platform operator wallet** | High — can write suppliers/products, record traceability | Low–Med | Hot wallet only has `PLATFORM_OPERATOR_ROLE`, **cannot** issue/revoke certs; KMS-backed key (Key Vault) with daily rotation, 2FA on every send, $1k daily gas limit; key never leaves the HSM | Indexer alerts on rate of new suppliers/products > 3σ; ops dashboard with daily volume baseline | Rotate key; replay any unconfirmed txs; timelock admin re-grants role to a new key |
| **Compromised certifier wallet** | High — can issue fake certs | Low | Certifier keys are **cold** (offline signing, e.g. Ledger), geographically separated, with per-cert gas cap; cert body pinned to IPFS + `metadataHash` on-chain; second-factor for any cert value > $5k | Indexer flags certs issued by a key that was rotated within 24h | Certifier revokes all recent certs; key rotated off-chain; new key re-onboarded via timelock |
| **IPFS CID swap / gateway tampering** | High — wrong cert body served to consumer | Low | `metadataHash` on-chain = `SHA-256(cid ‖ fields)`. On read, recompute and compare. CID is verified byte-for-byte on every download. | Integrity check fails → `/verify` page shows "Document integrity check failed" | Consumer sees the truth; ops investigates the gateway; we can switch gateway via config |
| **Malicious file upload** (PDF bomb, XSS in SVG, executable) | Med | Med | MIME allow-list (`image/jpeg,image/png,image/webp,application/pdf`); size cap (default 20 MiB); magic-byte sniff; ClamAV scan in Phase 6; **never serve user-uploaded HTML/SVG**; PDFs are rendered in a sandboxed viewer in Phase 5 | AV scanner flags; `ContentValidator` rejects at the API edge | Quarantine CID; alert ops; ban uploader wallet |
| **Replay of old cert** | High | Med | Every cert is bound to `(productId, certifier, chainNonce)`; tx-level nonce is managed by `TransactionQueue`; events are idempotent on `(txHash, logIndex)` | Indexer dedupes; duplicate-write attempt is rejected at the contract | n/a — replay cannot happen |
| **Reorg dropping a `CertificateRevoked`** | High (consumer sees stale "Verified") | Very Low (Polygon's finality is 64 blocks) | Indexer stores `blockHash` per row; `ChainReorgHandler` (Phase 3) re-fetches canonical block at height and rolls back any rows with a different hash; on Polygon this is essentially never seen in practice | Reorg watcher | Re-queue the revoke tx; resubmit |
| **Frontend QR replaced with phishing URL** | Med | Low | QR encodes `https://verify.halalchain.example/product/{productId}` — a known URL. The page is on our HTTPS domain. The productId is also a 32-byte keccak256 hash, hard to guess. The page also cross-checks `metadataHash` and warns on mismatch. | Phishing reports from users | Revoke phishing page; educate consumers to type the URL |
| **Database tampering** (operator or attacker modifies the off-chain mirror) | High | Med | Mirror tables are **append-only** for compliance events (no UPDATE/DELETE except via the indexer with a tx reference). Nightly hash chain over the compliance log. The chain is the source of truth: the indexer can replay from a checkpoint | Anomaly detection on out-of-band writes; row counts vs event log counts | Replay indexer from a checkpoint; restore from the chain |
| **API compromise (read)** | Med | Med | Public reads are **only** the verification endpoint, which is designed to be safe to expose; private endpoints require JWT + role check; rate-limited; audit log of every read | Anomaly on read patterns | Rotate signing keys; revoke JWTs |
| **RPC manipulation** (returns wrong data) | High (consumer misled) | Very Low | Public `/verify` reads from **multiple** public RPCs (Polygon + Base); if they disagree, show "Verification inconclusive" | Disagreement alert; chain health check | Consumer uses the chain that matches our pin; we surface the disagreement |
| **Side-channel via gas usage / mempool** | Low | Low | Sensitive writes (e.g., the 2-step rotation timelock) are public on-chain; the second step (execute after 24h) is the only public reveal. We use fresh addresses for sensitive operations to avoid linkage | n/a | n/a |
| **Reentrancy on `setCurrentCertificate`** | Critical | Very Low (Solidity 0.8.24 + checks-effects-interactions) | Function is `nonReentrant`; cert body fields are immutable after issue; product struct mutations are last (checks-effects-interactions pattern) | n/a | n/a |
| **Integer overflow in counters** | Low | Very Low | Solidity 0.8.24 has built-in overflow checks | n/a | n/a |
| **PII leak via IPFS metadata dict** | High | Med | `ContentValidator` and the `IStorageService` strip the metadata dict before pinning; only `name` and `keyvalues` are allowed; nothing sensitive goes in | n/a | n/a |
| **PII leak via `wallet` field on chain** | Med | Low | The wallet is a pseudonymous identifier. The link from wallet → real person lives off-chain in our DB (encrypted at rest). | n/a | Rotate wallet on link leak |
| **DNS / TLS hijack on `/verify` page** | High | Very Low | TLS pinning in mobile apps (Phase 5); HSTS preloaded; CAA records restricting CAs | Cert transparency monitoring | n/a |
| **Front-running of `issueCertificate`** | Low | Low | No price/value in the tx; the cert body is already on IPFS. The order of cert issue events is not financially significant | n/a | n/a |
| **Long-range attack on cert** | Low | Very Low | Polygon finality is 64 blocks (~2 min). Long-range attacks require controlling >50% of validator stake over a long window. | Finality confirmation in the indexer | n/a |
| **Malicious upgrade of HalalAccessControl or registries** | Critical | Very Low | UUPS proxy; upgrade callable only by `DEFAULT_ADMIN_ROLE` via the timelock. The new implementation is verified against the same Foundry test suite before deployment. | Every upgrade emits `Upgraded` event; ops reviews all upgrades manually | Re-deploy the previous implementation; timelock queue cancels pending upgrades |

## Detection surfaces

| Surface | What it watches |
|---|---|
| `Indexer:EventDispatcher` | Unknown event topics, decode failures, rate anomalies |
| `OutboxDispatcherService` | Tx timeouts, high failure rate, gas cap hits |
| `BackgroundService` heartbeat | All hosted services report liveness every 30s; missed heartbeats page the on-call |
| `Microsoft.Extensions.Diagnostics.HealthChecks` | `/health/ready` returns 503 if any of: chain RPC, IPFS, DB, or wallet KMS is unhealthy |
| Log aggregation (Seq or ELK in production) | All `LogError` from the blockchain module; all `LogWarning` from the indexer with `rate>3σ` markers |

## Incident response runbook

See `docs/runbooks/`:

- `chain-reorg-recovery.md` — what to do when the indexer detects a reorg.
- `compromised-key.md` — how to rotate a hot-wallet key in an emergency.
- `certifier-revoked-mid-rotation.md` — handling a certifier role revocation that lands mid-batch.
