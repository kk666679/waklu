"""Episodic memory: event and experience storage.

Reuses the canonical ``agent_memory.db``; types are scoped via
the ``type`` column (value ``episodic``) and tags can include a
``chain_id`` / ``parent_id`` for event reconstruction.
"""
from __future__ import annotations

import json
import os
import sqlite3
from datetime import datetime
from typing import Any, Dict, List, Optional

from .memory_manager import (
    MemoryEntry,
    MemoryStatus,
    MemoryType,
    _ensure_schema,
    _new_entry_id,
)
from .short_term_memory import _row_to_entry


class EpisodicMemory:
    def __init__(self, config: Dict[str, Any]) -> None:
        self.config = config
        self.max_episodes = config.get("capacity", {}).get("max_episodes", 10_000)
        self.db_path = config.get("storage", {}).get(
            "database", ".autoclaw/memory/agent_memory.db"
        )
        self.summary_path = config.get("storage", {}).get(
            "summary_path", ".autoclaw/memory/episodic/summaries/"
        )
        os.makedirs(self.summary_path, exist_ok=True)
        self.conn = sqlite3.connect(self.db_path, isolation_level=None)
        self.conn.row_factory = sqlite3.Row
        _ensure_schema(self.conn)

    def record(
        self,
        agent: str,
        action: str,
        input_: Dict[str, Any],
        output: Dict[str, Any],
        duration: int = 0,
        importance: float = 0.5,
        context: Optional[Dict[str, Any]] = None,
        tags: Optional[List[str]] = None,
        parent_id: Optional[str] = None,
        chain_id: Optional[str] = None,
        source: str = "agent",
    ) -> MemoryEntry:
        if self._active_episode_count() >= self.max_episodes:
            self._summarise_oldest()

        now = datetime.now()
        value = {
            "agent": agent,
            "action": action,
            "input": input_,
            "output": output,
            "duration": duration,
            "context": context or {},
            "parent_id": parent_id,
            "chain_id": chain_id,
        }
        ep_tags = list(tags or []) + ["episodic", f"agent:{agent}"]
        if chain_id:
            ep_tags.append(f"chain:{chain_id}")
        key = f"episode:{chain_id or 'ad-hoc'}:{now.isoformat()}"
        entry = MemoryEntry(
            id=_new_entry_id(key, "ep"),
            key=key,
            value=value,
            type=MemoryType.EPISODIC,
            importance=importance,
            confidence=1.0,
            created_at=now,
            updated_at=now,
            ttl=0,
            access_count=0,
            last_accessed=now,
            source=source,
            tags=ep_tags,
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
        return entry

    def store(self, key, value, ttl=None, importance=0.5, tags=None, source="agent"):
        """Generic store for consistency with the manager interface."""
        return self.record(
            agent=str(value.get("agent", "unknown")),
            action=str(value.get("action", "unknown")),
            input_=value.get("input", {}),
            output=value.get("output", {}),
            importance=importance,
            tags=tags,
            source=source,
        )

    def retrieve(self, key: str) -> Optional[MemoryEntry]:
        cur = self.conn.execute(
            "SELECT * FROM memory_entries WHERE key = ? AND type = 'episodic'",
            (key,),
        )
        row = cur.fetchone()
        return _row_to_entry(row) if row else None

    def search(self, query: str, limit: int = 10, min_score: float = 0.5):
        pattern = f"%{query}%"
        cur = self.conn.execute(
            """
            SELECT * FROM memory_entries
            WHERE type = 'episodic' AND status = 'active'
              AND (key LIKE ? OR value LIKE ? OR tags LIKE ?)
            ORDER BY created_at DESC LIMIT ?
            """,
            (pattern, pattern, pattern, limit),
        )
        results = []
        for row in cur.fetchall():
            entry = _row_to_entry(row)
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

    def temporal_range(self, start: datetime, end: datetime) -> List[MemoryEntry]:
        cur = self.conn.execute(
            """
            SELECT * FROM memory_entries
            WHERE type = 'episodic' AND status = 'active'
              AND created_at >= ? AND created_at <= ?
            ORDER BY created_at ASC
            """,
            (start.isoformat(), end.isoformat()),
        )
        return [_row_to_entry(row) for row in cur.fetchall()]

    def delete(self, key: str) -> bool:
        cur = self.conn.execute(
            "UPDATE memory_entries SET status = 'deleted' WHERE key = ? AND type = 'episodic'",
            (key,),
        )
        return cur.rowcount > 0

    def cleanup(self) -> None:
        # Episodes have ttl=0; we never expire them, but the spec asks
        # for a cleanup hook so that decayed episodes can be pruned.
        return None

    def _active_episode_count(self) -> int:
        cur = self.conn.execute(
            "SELECT COUNT(*) AS c FROM memory_entries WHERE type = 'episodic' AND status = 'active'"
        )
        return cur.fetchone()["c"] or 0

    def _summarise_oldest(self) -> None:
        cur = self.conn.execute(
            """
            SELECT key, value FROM memory_entries
            WHERE type = 'episodic' AND status = 'active'
            ORDER BY created_at ASC LIMIT ?
            """,
            (self.config.get("summarization", {}).get("interval", 100),),
        )
        rows = cur.fetchall()
        if not rows:
            return
        summary = {
            "summarised_keys": [row["key"] for row in rows],
            "count": len(rows),
            "generated_at": datetime.now().isoformat(),
        }
        path = os.path.join(self.summary_path, f"summary-{summary['generated_at']}.json")
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(summary, fh, indent=2)
        self.conn.executemany(
            "UPDATE memory_entries SET status = 'archived' WHERE key = ?",
            [(row["key"],) for row in rows],
        )

    def get_stats(self) -> Dict[str, Any]:
        cur = self.conn.execute(
            "SELECT COUNT(*) AS c, AVG(importance) AS a FROM memory_entries WHERE type='episodic'"
        )
        row = cur.fetchone()
        return {
            "total_episodes": row["c"] or 0,
            "avg_importance": row["a"] or 0.0,
            "max_episodes": self.max_episodes,
        }
