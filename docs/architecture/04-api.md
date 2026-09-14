# API Reference — Blockchain & IPFS layer

All endpoints are served by the existing `HalalChain.Platform.Api` (C# / .NET 10). The auth model is unchanged: vendor endpoints require a JWT, the public `/verify/{id}` is anonymous, ops endpoints require the operator role.

## Base URL
`https://api.halalchain.example/api/v1` (or `http://localhost:5001/api/v1` in dev)

## 1. Public verification (no auth)

### `GET /verify/{productId}`
The consumer-facing endpoint. Returns the trust signal by reading the chain directly and augmenting with our indexer cache for friendly fields.

**Path params**
- `productId` (string) — e.g. `HPC-PROD-2026-000123`

**Response 200**
```json
{
  "productId": "HPC-PROD-2026-000123",
  "source": "chain",
  "product": {
    "supplierId": "SUP-MY-000123",
    "ipfsCid": "bafy...",
    "jurisdiction": "MY",
    "currentCertId": "CRT-2026-JAKIM-000001",
    "status": "Verified",
    "registeredAtUnix": 1735689600,
    "updatedAtUnix": 1735689600
  },
  "certificate": {
    "certId": "CRT-2026-JAKIM-000001",
    "certifier": "0xJakim...",
    "country": "MY",
    "documentCid": "bafy...",
    "issuedAtUnix": 1735689600,
    "expiresAtUnix": 1767225600,
    "status": "Active"
  },
  "supplier": {
    "supplierId": "SUP-MY-000123",
    "wallet": "0x...",
    "jurisdiction": "MY",
    "status": "Active"
  },
  "warnings": [],
  "verifiedAt": "2026-08-25T20:00:00Z"
}
```

**Response 404** — product not found on chain
```json
{ "error": "Product not found on chain", "productId": "HPC-PROD-2026-000123" }
```

**Response 503** — chain unreachable AND no cached data
```json
{ "error": "Chain unreachable and no cached data", "productId": "HPC-PROD-2026-000123" }
```

**Warnings field semantics**
- `"Product RECALLED"` — chain says status=Recalled
- `"Certificate expired"` — `expiresAt < now` (even if cert status is still Active)
- `"Chain unreachable, showing last-known state"` — read failed, response is from cache
- `"Cache may be stale"` — cache row is > 5 min old

## 2. Vendor / operator (requires JWT)

### `POST /api/v1/suppliers`
Register a new supplier. Writes to the chain via the outbox.

**Auth:** `PLATFORM_OPERATOR_ROLE`

**Body**
```json
{
  "supplierId": "SUP-MY-000123",
  "wallet": "0x1234...",
  "ipfsMetadataCid": "bafy...",
  "jurisdiction": "MY"
}
```

**Response 202**
```json
{
  "supplierId": "SUP-MY-000123",
  "txHash": "0xpending...",
  "status": "Requested",
  "submittedAt": "2026-08-25T20:00:00Z"
}
```

### `GET /api/v1/suppliers/{supplierId}/status`
**Auth:** `PLATFORM_OPERATOR_ROLE`

**Response 200**
```json
{
  "supplierId": "SUP-MY-000123",
  "txHash": "0xabc...",
  "status": "Confirmed",
  "blockNumber": 54321000,
  "error": null
}
```

### `POST /api/v1/products`
**Auth:** `PLATFORM_OPERATOR_ROLE`

**Body**
```json
{
  "productId": "HPC-PROD-2026-000123",
  "supplierId": "SUP-MY-000123",
  "metadataHashHex": "0xabc...",
  "ipfsCid": "bafy...",
  "jurisdiction": "MY"
}
```

**Response 202** — same shape as the supplier response.

### `POST /api/v1/certificates/issue`
**Auth:** `PLATFORM_OPERATOR_ROLE` (sends the tx; the certifier is the on-chain `msg.sender` if their private key signs, but in our model the operator wallet sends and the certifier is recorded as the `certifier` field via off-chain attestation — see Phase 3 for the certifier-direct signing flow)

**Body**
```json
{
  "certId": "CRT-2026-JAKIM-000001",
  "productId": "HPC-PROD-2026-000123",
  "documentCid": "bafy...",
  "expiresAt": "2027-08-25T00:00:00Z",
  "scopeCid": "bafy...",
  "country": "MY"
}
```

**Response 202** — same as above.

### `POST /api/v1/certificates/{certId}/revoke`
**Auth:** `PLATFORM_OPERATOR_ROLE` OR the issuing certifier's wallet

**Body**
```json
{ "reasonCid": "bafy..." }
```

### `POST /api/v1/products/{productId}/recall`
**Auth:** `PLATFORM_OPERATOR_ROLE`

**Body**
```json
{ "reasonCid": "bafy..." }
```

### `POST /api/v1/traceability`
**Auth:** `PLATFORM_OPERATOR_ROLE` OR `INSPECTOR_ROLE`

**Body**
```json
{
  "eventId": "EVT-2026-000001",
  "productId": "HPC-PROD-2026-000123",
  "batchId": "BATCH-2026-001",
  "eventType": 0,
  "locationCid": "bafy...",
  "evidenceCid": "bafy...",
  "notesCid": "bafy..."
}
```

## 3. IPFS

### `POST /api/v1/ipfs/upload`
**Auth:** vendor or operator (depending on product)

**Body** (multipart/form-data)
- `file` — the document (image, PDF)
- `contentType` — must be in the allow-list (`image/jpeg,image/png,image/webp,application/pdf`)
- `encrypt` (optional bool, default false) — if true, AES-256-GCM-encrypt before upload
- `metadata` (optional JSON object) — non-sensitive key/value pairs to attach

**Response 200**
```json
{
  "cid": "bafy...",
  "sizeBytes": 12345,
  "provider": "pinata"
}
```

### `GET /api/v1/ipfs/{cid}`
**Auth:** none for public metadata; signed-URL flow for encrypted

Proxies the IPFS gateway and verifies CID integrity. Returns 400 if the byte stream doesn't match the CID.

## 4. Ops (requires `PLATFORM_OPERATOR_ROLE`)

### `GET /api/v1/blockchain/transactions/{txHash}`
Returns the outbox row for a tx (status, blockNumber, error).

### `GET /api/v1/blockchain/health`
Returns chain health: block number, last-indexed block, outbox pending count.

```json
{
  "chain": { "blockNumber": 54321000, "healthy": true },
  "indexer": { "lastIndexedBlock": 54320900, "lag": 100 },
  "outbox": { "pending": 0, "failed": 0 },
  "ipfs": { "provider": "pinata", "healthy": true }
}
```

## 5. Status codes

| Code | When |
|---|---|
| 200 | Success (read) or sync write result |
| 202 | Async write enqueued (durable); `txHash` returned in body for tracking |
| 400 | Validation error (bad productId, disallowed MIME, etc.) |
| 401 | Missing or invalid JWT |
| 403 | JWT lacks the required role |
| 404 | Product / supplier / certificate not found on chain |
| 409 | Duplicate (e.g. metadataHash already used) |
| 503 | Chain unreachable AND no cached data; or IPFS unavailable for an upload |

## 6. Rate limits

| Endpoint | Limit |
|---|---|
| `GET /verify/{id}` | 60/min per IP (consumer scans) |
| `POST /ipfs/upload` | 100/min per user |
| `POST /suppliers`, `POST /products`, `POST /certificates/*`, `POST /products/{id}/recall`, `POST /traceability` | 30/min per user (chain write cost) |
