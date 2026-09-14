# HalalChain Platform — Target Architecture

## Core principle

> AI discovers and interprets evidence. Deterministic systems decide business and compliance outcomes.

This separation is non-negotiable. It gives HalalChain a defensible product story beyond "AI-powered halal marketplace."

## Architecture

```
Customers / Vendors
        │
Marketplace / Web Frontends (MVC + Razor + future PWA)
        │ HTTPS/JWT
Platform API (.NET 10) — modular monolith
        │
        ├── Modules/Identity
        ├── Modules/Vendors
        ├── Modules/Catalog        ← PostgreSQL-backed (MVP 1)
        ├── Modules/Halal          ← Certificate lifecycle + Policy Engine
        ├── Modules/Commerce       ← Cart, multi-vendor orders, checkout
        ├── Modules/AI             ← Orchestration layer (never invents catalog data)
        ├── Infrastructure
        └── Persistence (EF Core + Npgsql)
        │
        ├── PostgreSQL             ← Catalog, Vendors, Orders, Halal records
        ├── Redis                  ← Cart session, cache
        │
        ├── ai-inference (Node.js) ← embeddings, summarize, classify
        └── tawheed (Python)       ← Multi-agent evidence collection + Policy Engine
```

## Halal verification flow

```
AI Agents (tawheed)
    │
    ├── Certificate evidence
    ├── Ingredient evidence
    └── Supplier evidence
            │
        Evidence Store
            │
    Deterministic Policy Engine
            │
    ┌───────┴───────┐
 VERIFIED      MANUAL_REVIEW / REJECT
```

The LLM is used **only** for document interpretation when structure is ambiguous.
It **never** assigns or overrides a compliance status.

## Halal certificate lifecycle

```
SUBMITTED → DOCUMENT_REVIEW → VERIFICATION → VERIFIED
                                                │
                                    ┌───────────┼───────────┐
                                 90 days     30 days     expiry
                               EXPIRING_SOON  EXPIRING   EXPIRED
```

On EXPIRED: product cannot display "HALAL VERIFIED" until a new valid certificate is verified.

## Multi-vendor order model

```
Cart (per customer)
 ├── Vendor A items
 ├── Vendor B items
 └── Vendor C items
        │ checkout
MasterOrder
 ├── VendorOrder A  (independent fulfil / ship / return / payout)
 ├── VendorOrder B
 └── VendorOrder C
```

## AI shopping flow (MVP 2)

```
Natural language query
    → Intent extraction
    → Budget / category / halal filter
    → PostgreSQL catalog query (real data only)
    → Inventory + price filter
    → Ranking
    → LLM explanation of retrieved results
```

The model explains real catalog records. It never invents product data.

## Domain events

```
ProductCreated          HalalVerificationCompleted
CertificateSubmitted    CertificateExpiring
OrderCreated            VendorOrderCreated
PaymentCaptured         ShipmentCreated
OrderDelivered          RefundRequested
```

## Roadmap

### MVP 1 — Trust + Marketplace Foundation
PostgreSQL catalog, vendor onboarding, halal certificate management, verification workflow,
cart, multi-vendor orders, basic checkout, vendor dashboard, admin dashboard.

### MVP 2 — AI Commerce
Semantic search, embeddings, catalog-grounded AI assistant, recommendations, vendor copilot.

### MVP 3 — Growth
Promotions, loyalty, affiliate, reviews, live commerce, notifications.

### MVP 4 — Regional
Malaysia (JAKIM), Indonesia (MUI/BPJPH), Singapore, Brunei, GCC, multi-currency, multi-language.
