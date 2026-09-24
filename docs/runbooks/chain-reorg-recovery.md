# Runbook — Chain reorg recovery

## Symptoms

- The indexer detects a reorg: a row in `ProductsOnChain` / `CertificatesOnChain` / etc. has a `blockHash` that no longer matches the canonical chain at that block height.
- An alert fires from Seq: "Reorg detected at block N, depth D".

## Triage

1. Confirm the reorg alert in the Seq dashboard and capture the affected block range.
2. Check the indexer lag and outbox health to verify whether the forked state is still being written.
3. Identify the affected `*OnChain` rows and whether the divergence is localized or broad across several tables.

## Mitigation

1. **Pause the indexer** to prevent it from reading more potentially-rolled-back state:
   ```bash
   az functionapp config appsettings set --name halalchain-api --settings Indexer__Paused=true
   ```
2. **Re-fetch canonical block hash** at the affected height:
   ```bash
   cast block <N> --rpc-url $POLYGON_RPC --json | jq .hash
   ```
3. **Compare** the hash to what we stored. If they differ, the rows in our DB for that block are now on a forked chain.
4. **Roll back** the affected rows by deleting them. (The mirror tables are append-only from the indexer; a delete is the roll-back signal.)
   ```sql
   DELETE FROM "ProductsOnChain"      WHERE "BlockNumber" BETWEEN N AND N + D;
   DELETE FROM "CertificatesOnChain"  WHERE "BlockNumber" BETWEEN N AND N + D;
   DELETE FROM "SuppliersOnChain"     WHERE "BlockNumber" BETWEEN N AND N + D;
   DELETE FROM "TraceabilityEventsOnChain" WHERE "BlockNumber" BETWEEN N AND N + D;
   ```
5. **Re-run the indexer** for the affected range. The `eth_getLogs` call will return the canonical events; the indexer re-inserts the correct rows.
6. **Verify**: `GET /api/v1/blockchain/health` shows `indexer.lag` returning to 0 and the affected products showing the correct state on `/verify/{id}`.
7. **Re-enable the indexer**.

## Detection
- The `EventIndexerHostedService` (Phase 3) runs `ChainReorgHandler` every 64 blocks.
- An alert fires from Seq: "Reorg detected at block N, depth D".

## Impact assessment
1. Open the Seq dashboard and filter `Blockchain:Reorg` for the last hour.
2. Identify the affected block range (reorgs on Polygon are typically depth 1–2 and self-heal within seconds).
3. List the rows in `*OnChain` tables with `BlockNumber >= N`.

## Recovery
1. **Pause the indexer** to prevent it from reading more potentially-rolled-back state:
   ```bash
   az functionapp config appsettings set --name halalchain-api --settings Indexer__Paused=true
   ```
   (In MVP this is a config flag the indexer checks; in Phase 3 it's a real kill switch.)
2. **Re-fetch canonical block hash** at the affected height:
   ```bash
   cast block <N> --rpc-url $POLYGON_RPC --json | jq .hash
   ```
3. **Compare** the hash to what we stored. If they differ, the rows in our DB for that block are now on a forked chain.
4. **Roll back** the affected rows by deleting them. (The mirror tables are append-only from the indexer; a delete is the roll-back signal.)
   ```sql
   DELETE FROM "ProductsOnChain"      WHERE "BlockNumber" BETWEEN N AND N + D;
   DELETE FROM "CertificatesOnChain"  WHERE "BlockNumber" BETWEEN N AND N + D;
   DELETE FROM "SuppliersOnChain"     WHERE "BlockNumber" BETWEEN N AND N + D;
   DELETE FROM "TraceabilityEventsOnChain" WHERE "BlockNumber" BETWEEN N AND N + D;
   ```
5. **Re-run the indexer** for the affected range. The `eth_getLogs` call will return the canonical events; the indexer re-inserts the correct rows.
6. **Verify**: `GET /api/v1/blockchain/health` shows `indexer.lag` returning to 0 and the affected products showing the correct state on `/verify/{id}`.
7. **Re-enable the indexer**.

## Why this is rare on Polygon
Polygon finality is 64 blocks (~2 min). A reorg deeper than 64 blocks on Polygon is essentially never seen. The outbox/queue system also means a tx that reorgs out is automatically re-sent on the canonical chain by the dispatcher.

## Related
- `docs/architecture/01-architecture.md` — the on-chain → indexer → DB flow.
- `HalalChain.Platform.Api/Modules/Indexer/` — the indexer implementation.
