"""Short-Term Memory implementation.

Stores entries in the canonical ``.autoclaw/memory/agent_memory.db``
with an in-memory LRU cache and TTL-based expiry.
"""
from __future__ import annotations

import json
import sqlite3
from collections import OrderedDict
from datetime import datetime
from typing import Any, Dict, List, Optional

from .memory_manager import (
    MemoryEntry,
    MemoryStatus,
    MemoryType,
    _ensure_schema,
    _new_entry_id,
)


def _row_to_entry(row: sqlite3.Row) -> MemoryEntry:
    return MemoryEntry(
        id=row["id"],
        key=row["key"],
        value=json.loads(row["value"]) if row["value"] else {},
        type=MemoryType(row["type"]),
        importance=row["importance"],
        confidence=row["confidence"],
        created_at=datetime.fromisoformat(row["created_at"]),
        updated_at=datetime.fromisoformat(row["updated_at"]),
        ttl=row["ttl"],
        access_count=row["access_count"],
        last_accessed=datetime.fromisoformat(row["last_accessed"]),
        source=row["source"],
        tags=json.loads(row["tags"]) if row["tags"] else [],
        status=MemoryStatus(row["status"]),
        summary=row["summary"] or "",
        version=row["version"] or 1,
        consolidated_from=json.loads(row["consolidated_from"]) if row["consolidated_from"] else [],
    )


class ShortTermMemory:
    """Working memory: LRU in-memory cache backed by SQLite."""

    def __init__(self, config: Dict[str, Any]) -> None:
        self.config = config
        self.max_entries = config.get("capacity", {}).get("max_entries", 1000)
        self.default_ttl = config.get("retention", {}).get("ttl", 3600)
        self.cache_size = config.get("storage", {}).get("cache_size", 100)
        self.db_path = config.get("storage", {}).get(
            "database", ".autoclaw/memory/agent_memory.db"
        )
        self.conn = sqlite3.connect(self.db_path, isolation_level=None)
        self.conn.row_factory = sqlite3.Row
        _ensure_schema(self.conn)
        self.cache: "OrderedDict[str, MemoryEntry]" = OrderedDict()
        self._load_cache()

    def _load_cache(self) -> None:
        cur = self.conn.execute(
            "SELECT * FROM memory_entries WHERE type = ? AND status = 'active' "
            "ORDER BY last_accessed DESC LIMIT ?",
            (MemoryType.SHORT_TERM.value, self.cache_size),
        )
        for row in cur.fetchall():
            entry = _row_to_entry(row)
            self.cache[entry.key] = entry

    @staticmethod
    def _is_expired(entry: MemoryEntry) -> bool:
        if entry.status == MemoryStatus.DELETED:
            return True
        if entry.ttl <= 0:
            return False
        return (datetime.now() - entry.created_at).total_seconds() > entry.ttl

    def store(
        self,
        key: str,
        value: Dict[str, Any],
        ttl: Optional[int] = None,
        importance: float = 0.5,
        tags: Optional[List[str]] = None,
        source: str = "agent",
    ) -> MemoryEntry:
        if len(self.cache) >= self.max_entries:
            self._evict_lru()

        now = datetime.now()
        entry = MemoryEntry(
            id=_new_entry_id(key, "stm"),
            key=key,
            value=value,
            type=MemoryType.SHORT_TERM,
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
        self.cache[entry.key] = entry
        self.cache.move_to_end(entry.key)
        return entry

    def retrieve(self, key: str) -> Optional[MemoryEntry]:
        if key in self.cache:
            entry = self.cache[key]
            if self._is_expired(entry):
                self.delete(key)
                return None
            entry.access_count += 1
            entry.last_accessed = datetime.now()
            self.cache.move_to_end(key)
            self._update_access(entry)
            return entry

        cur = self.conn.execute(
            "SELECT * FROM memory_entries WHERE key = ? AND status = 'active'",
            (key,),
        )
        row = cur.fetchone()
        if row is None:
            return None
        entry = _row_to_entry(row)
        if self._is_expired(entry):
            self.delete(key)
            return None
        entry.access_count += 1
        entry.last_accessed = datetime.now()
        self._update_access(entry)
        self.cache[entry.key] = entry
        self.cache.move_to_end(entry.key)
        return entry

    def search(
        self, query: str, limit: int = 10, min_score: float = 0.5
    ) -> List[Dict[str, Any]]:
        pattern = f"%{query}%"
        cur = self.conn.execute(
            """
            SELECT * FROM memory_entries
            WHERE status = 'active' AND (key LIKE ? OR value LIKE ?)
            ORDER BY importance DESC, last_accessed DESC
            LIMIT ?
            """,
            (pattern, pattern, limit),
        )
        results: List[Dict[str, Any]] = []
        for row in cur.fetchall():
            entry = _row_to_entry(row)
            if not self._is_expired(entry):
                # Cheap relevance score from query coverage of the key.
                score = sum(ch in (entry.key + json.dumps(entry.value)) for ch in query) / max(
                    1, len(query)
                )
                if score >= min_score:
                    results.append(
                        {
                            "entry": entry,
                            "similarity": score,
                            "relevance": score,
                            "importance": entry.importance,
                        }
                    )
        return results

    def delete(self, key: str) -> bool:
        self.cache.pop(key, None)
        cur = self.conn.execute(
            "UPDATE memory_entries SET status = 'deleted' WHERE key = ?", (key,)
        )
        return cur.rowcount > 0

    def _update_access(self, entry: MemoryEntry) -> None:
        self.conn.execute(
            """
            UPDATE memory_entries SET access_count = ?, last_accessed = ?
            WHERE key = ?
            """,
            (entry.access_count, entry.last_accessed.isoformat(), entry.key),
        )

    def _evict_lru(self) -> None:
        if not self.cache:
            return
        key, entry = self.cache.popitem(last=False)
        if entry.importance > 0.3:
            self.conn.execute(
                "UPDATE memory_entries SET status = 'consolidated' WHERE key = ?", (key,)
            )
        else:
            self.delete(key)

    def cleanup(self) -> None:
        cur = self.conn.execute(
            "SELECT key FROM memory_entries WHERE status = 'active'"
        )
        for row in cur.fetchall():
            self.cache.pop(row["key"], None)
        self.conn.execute(
            """
            UPDATE memory_entries SET status = 'expired'
            WHERE status = 'active' AND ttl > 0
              AND datetime(created_at, '+' || ttl || ' seconds') < datetime('now')
            """
        )

    def get_stats(self) -> Dict[str, Any]:
        cur = self.conn.execute(
            """
            SELECT
                COUNT(*) AS total,
                SUM(CASE WHEN status = 'active' THEN 1 ELSE 0 END) AS active,
                SUM(CASE WHEN status = 'expired' THEN 1 ELSE 0 END) AS expired,
                SUM(CASE WHEN status = 'consolidated' THEN 1 ELSE 0 END) AS consolidated,
                AVG(importance) AS avg_importance,
                AVG(access_count) AS avg_access
            FROM memory_entries
            """
        )
        row = cur.fetchone()
        return {
            "total_entries": row["total"] or 0,
            "active": row["active"] or 0,
            "expired": row["expired"] or 0,
            "consolidated": row["consolidated"] or 0,
            "avg_importance": row["avg_importance"] or 0.0,
            "avg_access_count": row["avg_access"] or 0.0,
            "cache_size": len(self.cache),
            "max_capacity": self.max_entries,
        }
