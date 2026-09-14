"""Procedural memory: cached skill executions.

Skills are described declaratively (steps + parameters) and we track
their success rate, average duration, and execution count. The cache
itself lives on disk under ``.autoclaw/memory/procedural/skills_cache``
for fast cold starts; metadata lives in the canonical DB.
"""
from __future__ import annotations

import json
import os
import sqlite3
import time
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


class ProceduralMemory:
    def __init__(self, config: Dict[str, Any]) -> None:
        self.config = config
        self.cache_path = config.get("storage", {}).get(
            "cache_path", ".autoclaw/memory/procedural/skills_cache/"
        )
        self.max_cached = config.get("storage", {}).get("max_cached_skills", 100)
        self.db_path = config.get("storage", {}).get(
            "skill_database", ".autoclaw/memory/agent_memory.db"
        )
        os.makedirs(self.cache_path, exist_ok=True)
        self.conn = sqlite3.connect(self.db_path, isolation_level=None)
        self.conn.row_factory = sqlite3.Row
        _ensure_schema(self.conn)

    def register_skill(
        self,
        name: str,
        description: str,
        steps: List[Dict[str, Any]],
        parameters: Optional[Dict[str, Any]] = None,
        tags: Optional[List[str]] = None,
    ) -> MemoryEntry:
        now = datetime.now()
        value = {
            "name": name,
            "description": description,
            "steps": steps,
            "parameters": parameters or {},
            "success_rate": 0.0,
            "avg_duration": 0.0,
            "execution_count": 0,
        }
        key = f"skill:{name}"
        entry = MemoryEntry(
            id=_new_entry_id(key, "skill"),
            key=key,
            value=value,
            type=MemoryType.PROCEDURAL,
            importance=0.6,
            confidence=1.0,
            created_at=now,
            updated_at=now,
            ttl=0,
            access_count=0,
            last_accessed=now,
            source="skill-registry",
            tags=list(tags or []) + ["skill", f"name:{name}"],
            status=MemoryStatus.ACTIVE,
        )
        self._upsert(entry)
        self._write_cache_file(entry)
        return entry

    def record_execution(
        self,
        skill_name: str,
        success: bool,
        duration: float,
    ) -> Optional[MemoryEntry]:
        cur = self.conn.execute(
            "SELECT * FROM memory_entries WHERE key = ? AND type = 'procedural'",
            (f"skill:{skill_name}",),
        )
        row = cur.fetchone()
        if row is None:
            return None
        entry = _row_to_entry(row)
        n = entry.value.get("execution_count", 0) + 1
        prev_rate = entry.value.get("success_rate", 0.0)
        prev_avg = entry.value.get("avg_duration", 0.0)
        entry.value["success_rate"] = prev_rate + ((1.0 if success else 0.0) - prev_rate) / n
        entry.value["avg_duration"] = prev_avg + (duration - prev_avg) / n
        entry.value["execution_count"] = n
        entry.last_accessed = datetime.now()
        entry.access_count += 1
        entry.updated_at = datetime.now()
        self._upsert(entry)
        return entry

    def _upsert(self, entry: MemoryEntry) -> None:
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

    def _write_cache_file(self, entry: MemoryEntry) -> None:
        path = os.path.join(self.cache_path, f"{entry.value.get('name', entry.id)}.json")
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(entry.value, fh, indent=2)

    def store(self, key, value, ttl=None, importance=0.5, tags=None, source="agent"):
        return self.register_skill(
            name=str(value.get("name", key)),
            description=str(value.get("description", "")),
            steps=value.get("steps", []),
            parameters=value.get("parameters"),
            tags=tags,
        )

    def retrieve(self, key: str) -> Optional[MemoryEntry]:
        cur = self.conn.execute(
            "SELECT * FROM memory_entries WHERE key = ? AND type = 'procedural'",
            (key,),
        )
        row = cur.fetchone()
        return _row_to_entry(row) if row else None

    def search(self, query: str, limit: int = 10, min_score: float = 0.5):
        pattern = f"%{query}%"
        cur = self.conn.execute(
            """
            SELECT * FROM memory_entries
            WHERE type = 'procedural' AND status = 'active'
              AND (key LIKE ? OR value LIKE ?)
            ORDER BY importance DESC LIMIT ?
            """,
            (pattern, pattern, limit),
        )
        results = []
        for row in cur.fetchall():
            entry = _row_to_entry(row)
            results.append(
                {
                    "entry": entry,
                    "similarity": 0.5,
                    "relevance": 0.5,
                    "importance": entry.importance,
                }
            )
        return results

    def delete(self, key: str) -> bool:
        cur = self.conn.execute(
            "UPDATE memory_entries SET status = 'deleted' WHERE key = ? AND type = 'procedural'",
            (key,),
        )
        return cur.rowcount > 0

    def cleanup(self) -> None:
        return None

    def get_stats(self) -> Dict[str, Any]:
        cur = self.conn.execute(
            "SELECT COUNT(*) AS c FROM memory_entries WHERE type = 'procedural'"
        )
        return {
            "total_skills": cur.fetchone()["c"] or 0,
            "max_cached": self.max_cached,
            "cache_path": self.cache_path,
        }
