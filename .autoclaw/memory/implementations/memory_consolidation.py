"""Memory consolidation: transfer STM entries to LTM.

The importance-scoring formula is configurable from
``memory_config.yaml`` -> ``consolidation.importance_scoring``.
"""
from __future__ import annotations

import json
from datetime import datetime
from typing import Any, Dict, List, Optional

try:
    import yaml  # type: ignore
except ImportError:  # pragma: no cover
    yaml = None  # type: ignore[assignment]

from .memory_manager import MemoryEntry, MemoryStatus, _new_entry_id


class MemoryConsolidator:
    def __init__(self, short_term_memory, long_term_memory) -> None:
        self.stm = short_term_memory
        self.ltm = long_term_memory
        self.config = self._load_config()

    @staticmethod
    def _load_config() -> Dict[str, Any]:
        if yaml is None:
            return MemoryConsolidator._default_config()
        try:
            with open(
                ".autoclaw/memory/memory_config.yaml", "r", encoding="utf-8"
            ) as fh:
                cfg = yaml.safe_load(fh) or {}
                return cfg.get("consolidation", {}) or MemoryConsolidator._default_config()
        except FileNotFoundError:
            return MemoryConsolidator._default_config()

    @staticmethod
    def _default_config() -> Dict[str, Any]:
        return {
            "enabled": True,
            "importance_scoring": {
                "enabled": True,
                "factors": {
                    "recency": 0.3,
                    "frequency": 0.2,
                    "relevance": 0.3,
                    "confidence": 0.2,
                },
                "threshold": 0.6,
            },
            "summarization": {
                "enabled": True,
                "max_tokens": 500,
                "preserve_key_facts": True,
            },
        }

    def calculate_importance(self, entry: MemoryEntry) -> float:
        cfg = self.config.get("importance_scoring", {})
        if not cfg.get("enabled", True):
            return entry.importance
        factors = cfg.get("factors", {})
        age = (datetime.now() - entry.created_at).total_seconds()
        recency = 1.0 / (1.0 + age / 86_400)
        frequency = min(entry.access_count / 10, 1.0)
        relevance = 0.5
        if any(t in entry.tags for t in ("halal", "verification")):
            relevance = 0.8
        if "critical" in entry.tags:
            relevance = 1.0
        confidence = entry.confidence
        return (
            factors.get("recency", 0.3) * recency
            + factors.get("frequency", 0.2) * frequency
            + factors.get("relevance", 0.3) * relevance
            + factors.get("confidence", 0.2) * confidence
        )

    def should_consolidate(self, entry: MemoryEntry) -> bool:
        if not self.config.get("enabled", True):
            return False
        threshold = self.config.get("importance_scoring", {}).get("threshold", 0.6)
        return self.calculate_importance(entry) >= threshold

    def consolidate_entry(self, entry: MemoryEntry) -> Optional[MemoryEntry]:
        if not self.should_consolidate(entry):
            return None
        importance = self.calculate_importance(entry)
        summary = self._generate_summary(entry) if self.config.get("summarization", {}).get("enabled", True) else None
        ltm_entry = self.ltm.store(
            key=entry.key,
            value=entry.value,
            importance=importance,
            tags=list(entry.tags) + ["consolidated"],
            source=f"consolidated_from_{entry.source}",
            summary=summary,
        )
        entry.status = MemoryStatus.CONSOLIDATED
        entry.updated_at = datetime.now()
        self.stm.conn.execute(
            "UPDATE memory_entries SET status = 'consolidated' WHERE key = ?",
            (entry.key,),
        )
        return ltm_entry

    def consolidate_batch(self, batch_size: int = 100) -> Dict[str, Any]:
        results: Dict[str, Any] = {
            "status": "success",
            "total_processed": 0,
            "consolidated": 0,
            "skipped": 0,
            "errors": [],
            "entries": [],
        }
        cur = self.stm.conn.execute(
            """
            SELECT * FROM memory_entries
            WHERE status = 'active'
            ORDER BY importance DESC, access_count DESC
            LIMIT ?
            """,
            (batch_size,),
        )
        for row in cur.fetchall():
            try:
                from .short_term_memory import _row_to_entry
                entry = _row_to_entry(row)
                ltm_entry = self.consolidate_entry(entry)
                if ltm_entry is not None:
                    results["consolidated"] += 1
                    results["entries"].append(
                        {"key": entry.key, "ltm_id": ltm_entry.id}
                    )
                else:
                    results["skipped"] += 1
                results["total_processed"] += 1
            except Exception as exc:  # pragma: no cover - defensive
                results["errors"].append(str(exc))
        return results

    def _generate_summary(self, entry: MemoryEntry) -> str:
        max_tokens = self.config.get("summarization", {}).get("max_tokens", 500)
        value_str = json.dumps(entry.value, indent=2)
        parts = [
            f"Key: {entry.key}",
            f"Type: {entry.type.value}",
            f"Source: {entry.source}",
            f"Value: {value_str[:200]}",
        ]
        summary = " | ".join(parts)
        if len(summary) > max_tokens:
            summary = summary[: max_tokens - 3] + "..."
        return summary
