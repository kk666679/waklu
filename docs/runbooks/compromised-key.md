# Runbook — Compromised hot-wallet key

## Symptoms

- A suspicious write appears in the chain from our operator wallet (for example, a `registerSupplier` for an unknown supplier).
- KMS audit log shows an anomalous `getSecret` call.
- Indexer alerts on rate > 3σ.

## Triage

1. Confirm the suspicious activity against the KMS audit log and the chain activity feed.
2. Establish whether the write is on the hot wallet or a delegated authority path.
3. Determine whether any unconfirmed transactions are still in the outbox and whether they were emitted using the compromised key.

## Mitigation

1. **Pause the outbox dispatcher** to prevent any new tx from being sent with the compromised key:
   ```bash
   az functionapp config appsettings set --name halalchain-api --settings Blockchain__OutboxPaused=true
   ```
2. **Rotate the key in KMS** — generate a new wallet, store the new private key in Key Vault under `Blockchain:HotWalletKey` (next-versioned secret).
3. **Restart the API** so it picks up the new key. The new wallet has no `PLATFORM_OPERATOR_ROLE` yet.
4. **Grant the new wallet the role** via the timelock:
   - The admin signs `proposeRoleGrant(newWallet, PLATFORM_OPERATOR_ROLE_ROLE)` and waits the 24h timelock.
   - On mainnet this means the platform has 24h of degraded write capability. Acceptable for a write that needs to be re-issued; product/cert reads work.
5. **Revoke the old wallet's role** via the same timelock.
6. **Investigate**: check the outbox for any unconfirmed txs from the old wallet, the indexer for events emitted under the old wallet, and the KMS audit log for the initial breach.
7. **File an incident report** — see the [CERT/CC](https://certcc.kb.cert.org/) template.

## Symptom
- A suspicious write appears in the chain from our operator wallet (e.g. a `registerSupplier` for an unknown supplier).
- KMS audit log shows an anomalous `getSecret` call.
- Indexer alerts on rate > 3σ.

## Immediate response (within minutes)
1. **Pause the outbox dispatcher** to prevent any new tx from being sent with the compromised key:
   ```bash
   az functionapp config appsettings set --name halalchain-api --settings Blockchain__OutboxPaused=true
   ```
2. **Rotate the key in KMS** — generate a new wallet, store the new private key in Key Vault under `Blockchain:HotWalletKey` (next-versioned secret).
3. **Restart the API** so it picks up the new key. The new wallet has no `PLATFORM_OPERATOR_ROLE` yet.
4. **Grant the new wallet the role** via the timelock:
   - The admin signs `proposeRoleGrant(newWallet, PLATFORM_OPERATOR_ROLE_ROLE)` and waits the 24h timelock.
   - On mainnet this means the platform has 24h of degraded write capability. Acceptable for a write that needs to be re-issued; product/cert reads work.
5. **Revoke the old wallet's role** via the same timelock.
6. **Investigate**: check the outbox for any unconfirmed txs from the old wallet, the indexer for events emitted under the old wallet, and the KMS audit log for the initial breach.
7. **File an incident report** — see the [CERT/CC](https://certcc.kb.cert.org/) template.

## What the compromise CAN and CANNOT do
- CAN: write suppliers, products, traceability events.
- **CANNOT:** issue or revoke certificates (the old wallet never had `CERTIFIER_ROLE`).
- **CANNOT:** upgrade the proxy (never had `DEFAULT_ADMIN_ROLE`).
- **CANNOT:** move funds out of the wallet (the wallet holds only MATIC for gas; the daily gas cap is enforced at the dispatch layer).

This is why the role split is the most important design decision: a compromise of the hot wallet is recoverable and bounded.

## After the rotation
- Replay any txs that were in the outbox at the time of compromise (they're durable; the new key will re-sign them and send).
- The indexer is unaffected — it reads from the chain by block, not by tx sender.
- Consumers see no change on `/verify/{id}`.
