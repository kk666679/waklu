"""Memory retrieval helpers with scoring and ranking."""
from __future__ import annotations

from datetime import datetime
from typing import Any, Dict, List, Optional

try:
    import yaml  # type: ignore
except ImportError:  # pragma: no cover
    yaml = None  # type: ignore[assignment]

from .memory_manager import MemoryEntry, MemoryType


class MemoryRetriever:
    def __init__(self, memory_manager) -> None:
        self.manager = memory_manager
        self.config = self._load_config()

    @staticmethod
    def _load_config() -> Dict[str, Any]:
        if yaml is None:
            return MemoryRetriever._default_config()
        try:
            with open(
                ".autoclaw/memory/memory_config.yaml", "r", encoding="utf-8"
            ) as fh:
                cfg = yaml.safe_load(fh) or {}
                return cfg.get("retrieval", {}) or MemoryRetriever._default_config()
        except FileNotFoundError:
            return MemoryRetriever._default_config()

    @staticmethod
    def _default_config() -> Dict[str, Any]:
        return {
            "default_limit": 10,
            "max_limit": 100,
            "scoring": {
                "recency_weight": 0.3,
                "relevance_weight": 0.4,
                "importance_weight": 0.3,
            },
            "fallback": {"enable": True, "strategy": "broaden_search"},
        }

    def retrieve_with_scoring(self, key: str) -> Optional[Dict[str, Any]]:
        entry = self.manager.retrieve(key)
        if entry is None:
            return None
        age = (datetime.now() - entry.created_at).total_seconds()
        recency = 1.0 / (1.0 + age / 604_800)
        access = min(entry.access_count / 10, 1.0)
        weights = self.config.get("scoring", {})
        total = (
            weights.get("recency_weight", 0.3) * recency
            + weights.get("relevance_weight", 0.4) * entry.confidence
            + weights.get("importance_weight", 0.3) * entry.importance
        )
        return {
            "entry": entry,
            "scores": {
                "recency": recency,
                "access": access,
                "confidence": entry.confidence,
                "importance": entry.importance,
                "total": total,
            },
        }

    def retrieve_recent(
        self, limit: int = 10, memory_type: Optional[MemoryType] = None
    ) -> List[MemoryEntry]:
        memory_type = memory_type or MemoryType.SHORT_TERM
        memory = self.manager._get_memory_system(memory_type)
        if memory is None:
            return []
        cur = memory.conn.execute(
            """
            SELECT * FROM memory_entries
            WHERE status = 'active'
            ORDER BY last_accessed DESC LIMIT ?
            """,
            (limit,),
        )
        from .short_term_memory import _row_to_entry
        return [_row_to_entry(row) for row in cur.fetchall()]

    def retrieve_important(
        self, min_importance: float = 0.7, limit: int = 10
    ) -> List[MemoryEntry]:
        results: List[MemoryEntry] = []
        for mem_type in (
            MemoryType.SHORT_TERM,
            MemoryType.LONG_TERM,
            MemoryType.EPISODIC,
            MemoryType.SEMANTIC,
        ):
            memory = self.manager._get_memory_system(mem_type)
            if memory is None:
                continue
            cur = memory.conn.execute(
                """
                SELECT * FROM memory_entries
                WHERE status = 'active' AND importance >= ?
                ORDER BY importance DESC LIMIT ?
                """,
                (min_importance, limit),
            )
            from .short_term_memory import _row_to_entry
            results.extend(_row_to_entry(row) for row in cur.fetchall())
        results.sort(key=lambda e: e.importance, reverse=True)
        return results[:limit]

    def retrieve_context(self, key: str, context_size: int = 5) -> List[Any]:
        entry = self.manager.retrieve(key)
        if entry is None:
            return []
        related: List[Any] = []
        for tag in list(entry.tags)[:3]:
            related.extend(self.manager.search(tag, limit=max(1, context_size // 2)))
        prefix = entry.key[:50] if len(entry.key) > 50 else entry.key
        related.extend(self.manager.search(prefix, limit=max(1, context_size // 2)))
        seen = set()
        unique: List[Any] = []
        for r in related:
            ent = r["entry"] if isinstance(r, dict) and "entry" in r else r
            ent_id = getattr(ent, "id", None) or (
                r.get("id") if isinstance(r, dict) else None
            )
            if ent_id and ent_id != entry.id and ent_id not in seen:
                seen.add(ent_id)
                unique.append(r)
        return unique[:context_size]

    def retrieve_temporal(
        self,
        start_time: datetime,
        end_time: datetime,
        memory_type: Optional[MemoryType] = None,
    ) -> List[MemoryEntry]:
        memory_type = memory_type or MemoryType.LONG_TERM
        memory = self.manager._get_memory_system(memory_type)
        if memory is None:
            return []
        cur = memory.conn.execute(
            """
            SELECT * FROM memory_entries
            WHERE status = 'active'
              AND created_at >= ? AND created_at <= ?
            ORDER BY created_at ASC
            """,
            (start_time.isoformat(), end_time.isoformat()),
        )
        from .short_term_memory import _row_to_entry
        return [_row_to_entry(row) for row in cur.fetchall()]
