---
name: halalchain-vector
description: Use the HalalChain CLI vector commands to search evidence, certificates, and supplier documents by semantic similarity.
metadata:
  sources:
    - docs/ARCHITECTURE.md
---

# HalalChain Vector Search

Run `halalchain vector search "<query>"` to find semantically similar evidence.

## What it searches

- Certificate text (extracted by the classifier agent)
- Supplier audit reports
- Agent traces (hashed provenance records)

## What it does not search

- The chain. On-chain records are integrity anchors, not a search index.
- Live product listings. Use `halalchain evaluate` for those.

## Common mistakes

- Treating a vector hit as a compliance decision. Vector search returns
  evidence; tawheed decides whether that evidence satisfies policy.
- Searching for "is this halal". That is not a semantic query — it is a
  verdict request, and it routes to `halalchain evaluate`, not `halalchain vector`.
