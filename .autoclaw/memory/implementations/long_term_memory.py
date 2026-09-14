"""Long-Term Memory implementation.

Persistent memory that reuses the canonical ``agent_memory.db`` and
the shared ``.autoclaw/vector/`` index. Embeddings are produced by
the local sentence-transformers model (see ``embeddings.py``).
"""
from __future__ import annotations

import json
import os
import sqlite3
from datetime import datetime
from typing import Any, Dict, List, Optional

from .embeddings import get_embedding, get_embedding_dim
from .memory_manager import (
    MemoryEntry,
    MemoryStatus,
    MemoryType,
    _ensure_schema,
    _new_entry_id,
)
from .short_term_memory import _row_to_entry


class LongTermMemory:
    def __init__(self, config: Dict[str, Any]) -> None:
        self.config = config
        self.max_entries = config.get("capacity", {}).get("max_entries", 1_000_000)
        self.default_ttl = config.get("retention", {}).get("ttl", 31_536_000)
        self.db_path = config.get("storage", {}).get(
            "database", ".autoclaw/memory/agent_memory.db"
        )
        self.vector_store_path = config.get("storage", {}).get(
            "vector_store", ".autoclaw/vector/embeddings.db"
        )
        self.min_similarity = config.get("search", {}).get("min_similarity", 0.7)
        os.makedirs(os.path.dirname(self.vector_store_path) or ".", exist_ok=True)
        self.conn = sqlite3.connect(self.db_path, isolation_level=None)
        self.conn.row_factory = sqlite3.Row
        _ensure_schema(self.conn)
        self._ensure_vector_table()

    def _ensure_vector_table(self) -> None:
        self.conn.execute(
            """
            CREATE TABLE IF NOT EXISTS vector_embeddings (
                entry_id TEXT PRIMARY KEY,
                embedding TEXT,
                model TEXT,
                dimensions INTEGER,
                created_at TEXT
            )
            """
        )

    def store(
        self,
        key: str,
        value: Dict[str, Any],
        ttl: Optional[int] = None,
        importance: float = 0.5,
        tags: Optional[List[str]] = None,
        source: str = "agent",
        summary: Optional[str] = None,
    ) -> MemoryEntry:
        cur = self.conn.execute(
            "SELECT COUNT(*) AS c FROM memory_entries WHERE status = 'active'"
        )
        if (cur.fetchone()["c"] or 0) >= self.max_entries:
            self._archive_oldest()

        now = datetime.now()
        entry = MemoryEntry(
            id=_new_entry_id(key, "ltm"),
            key=key,
            value=value,
            type=MemoryType.LONG_TERM,
            importance=importance,
            confidence=0.9,
            created_at=now,
            updated_at=now,
            ttl=ttl or self.default_ttl,
            access_count=0,
            last_accessed=now,
            source=source,
            tags=tags or [],
            status=MemoryStatus.ACTIVE,
            summary=summary or "",
        )
        self.conn.execute(
            """
            INSERT OR REPLACE INTO memory_entries
            (id, key, value, type, importance, confidence, created_at,
             updated_at, ttl, access_count, last_accessed, source, tags,
             status, summary, version, consolidated_from)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            (
                entry.id, entry.key, json.dumps(entry.value), entry.type.value,
                entry.importance, entry.confidence, entry.created_at.isoformat(),
                entry.updated_at.isoformat(), entry.ttl, entry.access_count,
                entry.last_accessed.isoformat(), entry.source,
                json.dumps(entry.tags), entry.status.value, entry.summary,
                entry.version, json.dumps(entry.consolidated_from),
            ),
        )

        embedding = get_embedding(json.dumps(value, sort_keys=True))
        self.conn.execute(
            """
            INSERT OR REPLACE INTO vector_embeddings
            (entry_id, embedding, model, dimensions, created_at)
            VALUES (?, ?, ?, ?, ?)
            """,
            (
                entry.id,
                json.dumps(embedding),
                "sentence-transformers/all-MiniLM-L6-v2",
                get_embedding_dim(),
                now.isoformat(),
            ),
        )
        return entry

    def retrieve(self, key: str) -> Optional[MemoryEntry]:
        cur = self.conn.execute(
            "SELECT * FROM memory_entries WHERE key = ? AND status IN ('active', 'archived')",
            (key,),
        )
        row = cur.fetchone()
        if row is None:
            return None
        entry = _row_to_entry(row)
        if self._is_expired(entry):
            self._archive_entry(key)
            return None
        entry.access_count += 1
        entry.last_accessed = datetime.now()
        self._update_access(entry)
        return entry

    def search(
        self, query: str, limit: int = 10, min_score: float = 0.5
    ) -> List[Dict[str, Any]]:
        query_vec = get_embedding(query)
        cur = self.conn.execute(
            """
            SELECT e.*, v.embedding
            FROM memory_entries e
            JOIN vector_embeddings v ON e.id = v.entry_id
            WHERE e.status = 'active' AND e.ttl > 0
            """
        )
        results: List[Dict[str, Any]] = []
        threshold = max(min_score, self.min_similarity)
        for row in cur.fetchall():
            entry = _row_to_entry(row)
            stored = json.loads(row["embedding"]) if row["embedding"] else []
            sim = self._cosine_similarity(query_vec, stored)
            if sim >= threshold:
                results.append(
                    {
                        "entry": entry,
                        "similarity": sim,
                        "relevance": sim * 0.6 + entry.importance * 0.4,
                        "importance": entry.importance,
                    }
                )
        results.sort(key=lambda r: r["relevance"], reverse=True)
        return results[:limit]

    @staticmethod
    def _cosine_similarity(a: List[float], b: List[float]) -> float:
        if not a or not b:
            return 0.0
        n = min(len(a), len(b))
        a, b = a[:n], b[:n]
        dot = sum(x * y for x, y in zip(a, b))
        na = sum(x * x for x in a) ** 0.5
        nb = sum(y * y for y in b) ** 0.5
        if na == 0 or nb == 0:
            return 0.0
        return dot / (na * nb)

    @staticmethod
    def _is_expired(entry: MemoryEntry) -> bool:
        if entry.status == MemoryStatus.DELETED:
            return True
        if entry.ttl <= 0:
            return False
        return (datetime.now() - entry.created_at).total_seconds() > entry.ttl

    def _update_access(self, entry: MemoryEntry) -> None:
        self.conn.execute(
            """
            UPDATE memory_entries SET access_count = ?, last_accessed = ?
            WHERE key = ?
            """,
            (entry.access_count, entry.last_accessed.isoformat(), entry.key),
        )

    def _archive_entry(self, key: str) -> None:
        self.conn.execute(
            "UPDATE memory_entries SET status = 'archived' WHERE key = ?", (key,)
        )

    def _archive_oldest(self) -> None:
        cur = self.conn.execute(
            """
            SELECT key FROM memory_entries WHERE status = 'active'
            ORDER BY importance ASC, last_accessed ASC LIMIT 1
            """
        )
        row = cur.fetchone()
        if row is not None:
            self._archive_entry(row["key"])

    def delete(self, key: str) -> bool:
        cur = self.conn.execute(
            "UPDATE memory_entries SET status = 'deleted' WHERE key = ?", (key,)
        )
        return cur.rowcount > 0

    def cleanup(self) -> None:
        self.conn.execute(
            """
            UPDATE memory_entries SET status = 'expired'
            WHERE status = 'active' AND ttl > 0
              AND datetime(created_at, '+' || ttl || ' seconds') < datetime('now')
            """
        )

    def apply_decay(self) -> None:
        decay_cfg = self.config.get("retention", {}).get("decay", {})
        if not decay_cfg.get("enabled", True):
            return
        half_life = decay_cfg.get("half_life", 31_536_000)
        factor = decay_cfg.get("importance_factor", 0.5)
        cur = self.conn.execute(
            "SELECT key, importance, created_at FROM memory_entries WHERE status = 'active'"
        )
        for row in cur.fetchall():
            age = (datetime.now() - datetime.fromisoformat(row["created_at"])).total_seconds()
            decay = 2 ** (-age / half_life)
            new_imp = row["importance"] * (1 - factor + factor * decay)
            if new_imp < 0.1:
                self._archive_entry(row["key"])
            else:
                self.conn.execute(
                    "UPDATE memory_entries SET importance = ? WHERE key = ?",
                    (new_imp, row["key"]),
                )

    def get_stats(self) -> Dict[str, Any]:
        cur = self.conn.execute(
            """
            SELECT
                COUNT(*) AS total,
                SUM(CASE WHEN status = 'active' THEN 1 ELSE 0 END) AS active,
                SUM(CASE WHEN status = 'archived' THEN 1 ELSE 0 END) AS archived,
                SUM(CASE WHEN status = 'expired' THEN 1 ELSE 0 END) AS expired,
                AVG(importance) AS avg_importance,
                AVG(access_count) AS avg_access
            FROM memory_entries
            """
        )
        row = cur.fetchone()
        return {
            "total_entries": row["total"] or 0,
            "active": row["active"] or 0,
            "archived": row["archived"] or 0,
            "expired": row["expired"] or 0,
            "avg_importance": row["avg_importance"] or 0.0,
            "avg_access_count": row["avg_access"] or 0.0,
            "max_capacity": self.max_entries,
            "vector_store": self.vector_store_path,
        }
