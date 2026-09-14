# HalalChain Memory System

> **PROPRIETARY & CONFIDENTIAL**
> This directory contains the multi-tier memory system for AutoClaw agents.
> Unauthorized use, disclosure, or distribution is strictly prohibited.

## Overview

The HalalChain memory system provides short-term working memory, long-term
persistent memory, and consolidation/retrieval helpers for AI agents.

## Architecture

```
                  HalalChain Memory System
  ┌─────────────────────────────────────────────────────────────┐
  │              Memory Manager (orchestrator)                  │
  └─────────────────────────────────────────────────────────────┘
       │            │              │             │           │
       ▼            ▼              ▼             ▼           ▼
   Short-Term   Long-Term      Episodic      Semantic    Procedural
   (SQLite +    (SQLite +      (events)      (graph +    (skills
    LRU cache)   vector index)                 inference)  cache)
```

All tiers share the canonical `agent_memory.db` and the shared
`.autoclaw/vector/` index. Embeddings are produced by a local
`sentence-transformers/all-MiniLM-L6-v2` model.

## Architectural principle

Per `AGENTS.md`, the memory layer **collects evidence**; the deterministic
Policy Engine in `.halalchain/tawheed` is the **only** component that may
assign a halal verdict. This memory layer must never assert or override a
verdict — it surfaces evidence and provenance for the Policy Engine.

## Layout

```
.autoclaw/memory/
├── memory_config.yaml          # main configuration
├── short_term/  long_term/  episodic/  semantic/  procedural/
│   └── config.yaml             # per-tier overrides
├── implementations/            # Python package
│   ├── __init__.py
│   ├── memory_manager.py
│   ├── short_term_memory.py
│   ├── long_term_memory.py
│   ├── episodic_memory.py
│   ├── semantic_memory.py
│   ├── procedural_memory.py
│   ├── memory_consolidation.py
│   ├── memory_retrieval.py
│   └── embeddings.py
└── README.md
```

## Quick commands

```bash
# store / retrieve / search via the manager CLI
python -m .autoclaw.memory.implementations.memory_manager \
  --action store --type short_term \
  --key "product_123" --value '{"verdict_evidence": "..."}'

# consolidate STM -> LTM
python -m .autoclaw.memory.implementations.memory_manager \
  --action consolidate

# stats
python -m .autoclaw.memory.implementations.memory_manager --action stats
```

Or as a library:

```python
from .autoclaw.memory.implementations import MemoryManager, MemoryType
mgr = MemoryManager()
mgr.store("k", {"evidence": "..."}, MemoryType.LONG_TERM, importance=0.9)
mgr.search("halal verification", MemoryType.LONG_TERM, limit=5)
```

## Version history

| Version | Date       | Changes |
|---------|------------|---------|
| 3.0.0   | 2026-08-27 | Multi-tier memory (STM + LTM + episodic + semantic + procedural), shared vector index, local sentence-transformers embeddings |
| 2.0.0   | prior      | Single-DB STM/LTM only |
| 1.0.0   | prior      | Initial memory config |

(c) HalalChain 2024-2026. All rights reserved.
