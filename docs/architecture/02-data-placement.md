# Data Placement — what goes on-chain, what goes to IPFS, what stays off-chain

The rule: **on-chain only what gives trust. IPFS only what is too large for chain. Off-chain DB only what is operational.**

## 1. Decision table

| Data | On-chain | IPFS (public) | IPFS (encrypted / private) | Off-chain DB | Never stored |
|---|---|---|---|---|---|
| Supplier ID (bytes32) | ✅ | | | mirror row | |
| Supplier wallet address | ✅ | | | mirror row | |
| Supplier status (Active/Suspended/Revoked) | ✅ | | | mirror row | |
| Supplier jurisdiction (ISO-2) | ✅ | | | | |
| Supplier IPFS metadata CID | ✅ | | | mirror | |
| Supplier legal name, address, contact | | | | ✅ (PII) | never on-chain, never public IPFS |
| Product ID | ✅ | | | mirror | |
| Product supplierId | ✅ | | | mirror | |
| Product metadataHash (SHA-256 of IPFS CID + fields) | ✅ | | | mirror | |
| Product IPFS CID | ✅ | | | mirror | |
| Product jurisdiction | ✅ | | | mirror | |
| Product currentCertId | ✅ | | | mirror | |
| Product status (Pending/Verified/Suspended/Recalled) | ✅ | | | mirror | |
| Product name, description, ingredients, allergens, nutrition | | ✅ | | mirror for search | |
| Product images | | ✅ | | | |
| Product price, inventory, SKU | | | | ✅ | |
| Certificate ID | ✅ | | | mirror | |
| Certificate issuer (certifier wallet) | ✅ | | | mirror | |
| Certificate issuedAt, expiresAt | ✅ | | | mirror | |
| Certificate document CID (IPFS pointer) | ✅ | | | mirror | |
| Certificate scope CID | ✅ | | | | |
| Certificate country | ✅ | | | | |
| Certificate status (Active/Revoked/Expired) | ✅ | | | mirror | |
| Certificate body (PDF/image) | | ✅ (public certifier logo, scope doc) | ✅ (sensitive: auditor names, PII, internal findings) | | |
| Traceability event hash | ✅ | | | mirror | |
| Traceability event details (location, evidence, notes) | | ✅ | | mirror | |
| Traceability batch ID, actor wallet, timestamp | ✅ | | | mirror | |
| Certifier MoU, KYC documents | | | ✅ (encrypted; certifier holds key) | | never public |
| Certifier real names of officers | | | | ✅ (off-chain, PII) | never on-chain, never IPFS |
| Certifier logo, public name, country | | ✅ (or CDN) | | ✅ | |
| Consumer PII (name, phone, email, address) | | | | ✅ | never on-chain, never IPFS, never logged on /verify |
| Order data (buyer, items, amounts) | | | | ✅ | never on-chain in MVP (escrow is Phase 4) |
| AI verification results (Tawheed agent) | | ✅ (hash on-chain) | | ✅ (full output) | |
| Vendor payout history | | | | ✅ | privacy |
| Hot-wallet private key | | | | **NEVER in DB** — KMS only | |

## 2. Why this split

### On-chain
- **Pseudonymous IDs only** (bytes32, keccak256 of human id like `SUP-MY-000123`).
- **No PII.** The supplier's real name, address, phone, email are off-chain in our DB.
- **Hashes & CIDs, not raw data.** The cert body is on IPFS; on-chain we only store the pointer.
- **Status flags, not full state.** A `Revoked` enum is a single byte; the whole certificate struct is <300 bytes.
- **Wallet addresses, not accounts.** Wallets are pseudonymous; the human is bound to the wallet off-chain.
- **Append-only / well-bounded writes.** No `setName`, no `setDescription` — those are off-chain and search-indexed.

### IPFS (public)
- Product metadata JSON (ingredients, allergens, nutrition, batch info).
- Product images and marketing media (fronted by a CDN with a `?cache=` query).
- Certifier logos and public scope documents.
- Traceability event details (location, evidence, notes).
- AI verification output (publicly viewable for transparency).
- **Always content-addressed** — the CID is `SHA-256(cidBytes)` and the on-chain `metadataHash` recomputes from the CID; tampering at the gateway is detectable.

### IPFS (private / encrypted)
- Cert body that contains auditor names, internal findings, or any PII.
- MoU, KYC, banking docs.
- **Encrypted with AES-256-GCM** before upload (the `StorageEncryption` helper). Only the CID of the ciphertext is on-chain; the key is held by the certifier + our KMS.

### Off-chain DB
- Everything operational: product names for search, prices, inventory, orders, analytics, vendor payouts, consumer carts.
- The on-chain mirror tables (`SupplierOnChain`, `ProductOnChain`, etc.) hold **only the on-chain fields** (IDs, hashes, status). They are append-only (no UPDATEs except from the indexer with a tx reference). Compliance audit trails are protected by the chain being the source of truth.

### Never stored (anywhere public)
- PII of consumers and certifier officers.
- Hot-wallet private keys (KMS only, in HSM-grade storage).
- Auditor identities when the audit could expose a whistleblower.

## 3. The privacy contract

| Who can see what | Public | On-chain | Encrypted IPFS (key held by) | Off-chain DB |
|---|---|---|---|---|
| Consumer scanning a QR | Product status, cert status, certifier, expiry, jurisdiction, "Last verified" timestamp | Same | Decrypted by us on the fly if they request the cert body | NO (no consumer data) |
| Vendor (their own products) | Same | Same | Their own (they hold the key for their docs) | YES (full PII) |
| Certifier (their certs) | Same | Same | Their own | YES |
| Platform operator (us) | Same | Same | YES (our KMS has the master key for compliance audits) | YES |
| Other vendors | Same | Same | NO | NO |
| Regulator (on subpoena) | Same | Same | YES (warrant-based key release) | YES |

## 4. GDPR right-to-erasure

IPFS is content-addressed and cannot delete. Therefore **PII is never put on IPFS or the chain**. Off-chain DB has the normal erasure flow. For a person who was a certifier officer and leaves: their name is in our off-chain DB only; deleting the row makes them anonymous on-chain (the certifier wallet remains, but it can rotate to a new officer's wallet via `proposeWalletRotation`).
