"""Memory Manager - Orchestrates all memory systems.

Copyright (c) HalalChain. All rights reserved.
"""
from __future__ import annotations

import json
import sqlite3
import hashlib
import time
from dataclasses import asdict, dataclass, field
from datetime import datetime
from enum import Enum
from typing import Any, Dict, List, Optional

try:
    import yaml  # type: ignore
except ImportError:  # pragma: no cover - config is best-effort
    yaml = None  # type: ignore[assignment]


class MemoryType(Enum):
    SHORT_TERM = "short_term"
    LONG_TERM = "long_term"
    EPISODIC = "episodic"
    SEMANTIC = "semantic"
    PROCEDURAL = "procedural"


class MemoryStatus(Enum):
    ACTIVE = "active"
    EXPIRED = "expired"
    CONSOLIDATED = "consolidated"
    ARCHIVED = "archived"
    DELETED = "deleted"


@dataclass
class MemoryEntry:
    id: str
    key: str
    value: Dict[str, Any]
    type: MemoryType
    importance: float
    confidence: float
    created_at: datetime
    updated_at: datetime
    ttl: int
    access_count: int
    last_accessed: datetime
    source: str
    tags: List[str]
    status: MemoryStatus
    summary: str = ""
    version: int = 1
    consolidated_from: List[str] = field(default_factory=list)

    def to_dict(self) -> Dict[str, Any]:
        d = asdict(self)
        d["type"] = self.type.value
        d["status"] = self.status.value
        d["created_at"] = self.created_at.isoformat()
        d["updated_at"] = self.updated_at.isoformat()
        d["last_accessed"] = self.last_accessed.isoformat()
        return d


def _new_entry_id(key: str, prefix: str = "mem") -> str:
    stamp = time.time_ns()
    return hashlib.md5(f"{prefix}:{key}:{stamp}".encode()).hexdigest()


def _ensure_schema(conn: sqlite3.Connection) -> None:
    """Create the unified schema in the canonical agent_memory.db."""
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS memory_entries (
            id TEXT PRIMARY KEY,
            key TEXT UNIQUE,
            value TEXT,
            type TEXT,
            importance REAL,
            confidence REAL,
            created_at TEXT,
            updated_at TEXT,
            ttl INTEGER,
            access_count INTEGER,
            last_accessed TEXT,
            source TEXT,
            tags TEXT,
            status TEXT,
            summary TEXT,
            version INTEGER,
            consolidated_from TEXT
        )
        """
    )
    for col in ("key", "status", "type", "importance", "created_at", "ttl"):
        conn.execute(
            f"CREATE INDEX IF NOT EXISTS idx_mem_{col} ON memory_entries({col})"
        )
    conn.commit()


class MemoryManager:
    """Orchestrates STM, LTM, episodic, semantic and procedural memories."""

    DEFAULT_CONFIG_PATH = ".autoclaw/memory/memory_config.yaml"

    def __init__(self, config_path: str = DEFAULT_CONFIG_PATH) -> None:
        self.config = self._load_config(config_path)
        self.short_term = None
        self.long_term = None
        self.episodic = None
        self.semantic = None
        self.procedural = None
        self._initialize_memory_systems()

    @staticmethod
    def _load_config(path: str) -> Dict[str, Any]:
        if yaml is None:
            return MemoryManager._default_config()
        try:
            with open(path, "r", encoding="utf-8") as fh:
                return yaml.safe_load(fh) or MemoryManager._default_config()
        except FileNotFoundError:
            return MemoryManager._default_config()

    @staticmethod
    def _default_config() -> Dict[str, Any]:
        return {
            "short_term": {"enabled": True, "capacity": {"max_entries": 1000}},
            "long_term": {"enabled": True, "capacity": {"max_entries": 1_000_000}},
            "episodic": {"enabled": True},
            "semantic": {"enabled": True},
            "procedural": {"enabled": True},
            "consolidation": {
                "importance_scoring": {"threshold": 0.6},
            },
        }

    def _initialize_memory_systems(self) -> None:
        from .short_term_memory import ShortTermMemory
        from .long_term_memory import LongTermMemory
        from .episodic_memory import EpisodicMemory
        from .semantic_memory import SemanticMemory
        from .procedural_memory import ProceduralMemory

        if self.config.get("short_term", {}).get("enabled", True):
            self.short_term = ShortTermMemory(self.config["short_term"])
        if self.config.get("long_term", {}).get("enabled", True):
            self.long_term = LongTermMemory(self.config["long_term"])
        if self.config.get("episodic", {}).get("enabled", True):
            self.episodic = EpisodicMemory(self.config["episodic"])
        if self.config.get("semantic", {}).get("enabled", True):
            self.semantic = SemanticMemory(self.config["semantic"])
        if self.config.get("procedural", {}).get("enabled", True):
            self.procedural = ProceduralMemory(self.config["procedural"])

    def store(
        self,
        key: str,
        value: Dict[str, Any],
        memory_type: MemoryType = MemoryType.SHORT_TERM,
        ttl: Optional[int] = None,
        importance: float = 0.5,
        tags: Optional[List[str]] = None,
        source: str = "agent",
    ) -> MemoryEntry:
        memory = self._get_memory_system(memory_type)
        if memory is None:
            raise ValueError(f"Memory type {memory_type} not enabled")

        entry = memory.store(key, value, ttl, importance, tags, source)

        threshold = (
            self.config.get("consolidation", {})
            .get("importance_scoring", {})
            .get("threshold", 0.6)
        )
        if memory_type == MemoryType.SHORT_TERM and importance > threshold:
            self._schedule_consolidation(entry)
        return entry

    def retrieve(
        self,
        key: str,
        memory_type: Optional[MemoryType] = None,
    ) -> Optional[MemoryEntry]:
        if memory_type is not None:
            memory = self._get_memory_system(memory_type)
            return memory.retrieve(key) if memory else None

        for mem_type in (
            MemoryType.SHORT_TERM,
            MemoryType.LONG_TERM,
            MemoryType.EPISODIC,
            MemoryType.SEMANTIC,
        ):
            memory = self._get_memory_system(mem_type)
            if memory is None:
                continue
            result = memory.retrieve(key)
            if result is not None:
                return result
        return None

    def search(
        self,
        query: str,
        memory_type: Optional[MemoryType] = None,
        limit: int = 10,
        min_score: float = 0.5,
    ) -> List[Any]:
        if memory_type is not None:
            memory = self._get_memory_system(memory_type)
            if memory is None:
                return []
            return memory.search(query, limit, min_score)

        results: List[Dict[str, Any]] = []
        per_type = max(1, limit // 2)
        for mem_type in (
            MemoryType.SHORT_TERM,
            MemoryType.LONG_TERM,
            MemoryType.EPISODIC,
            MemoryType.SEMANTIC,
            MemoryType.PROCEDURAL,
        ):
            memory = self._get_memory_system(mem_type)
            if memory is None or not hasattr(memory, "search"):
                continue
            for hit in memory.search(query, per_type, min_score):
                if isinstance(hit, dict):
                    results.append(hit)
                else:  # bare MemoryEntry fallback
                    results.append({"entry": hit, "similarity": 0.5, "importance": hit.importance})
        results.sort(
            key=lambda r: (r.get("similarity", 0.0) + r.get("importance", 0.0)) / 2,
            reverse=True,
        )
        return results[:limit]

    def consolidate(self, batch_size: int = 100) -> Dict[str, Any]:
        if self.short_term is None or self.long_term is None:
            return {"status": "error", "message": "Required memory systems not enabled"}
        from .memory_consolidation import MemoryConsolidator
        consolidator = MemoryConsolidator(self.short_term, self.long_term)
        return consolidator.consolidate_batch(batch_size)

    def forget(self, key: str, memory_type: MemoryType) -> bool:
        memory = self._get_memory_system(memory_type)
        if memory is None:
            return False
        return bool(memory.delete(key))

    def _get_memory_system(self, memory_type: MemoryType):
        return {
            MemoryType.SHORT_TERM: self.short_term,
            MemoryType.LONG_TERM: self.long_term,
            MemoryType.EPISODIC: self.episodic,
            MemoryType.SEMANTIC: self.semantic,
            MemoryType.PROCEDURAL: self.procedural,
        }.get(memory_type)

    def _schedule_consolidation(self, entry: MemoryEntry) -> None:
        # Hook for an external scheduler; intentionally a no-op.
        return None

    def get_stats(self) -> Dict[str, Any]:
        stats: Dict[str, Any] = {}
        for name, memory in (
            ("short_term", self.short_term),
            ("long_term", self.long_term),
            ("episodic", self.episodic),
            ("semantic", self.semantic),
            ("procedural", self.procedural),
        ):
            if memory is not None and hasattr(memory, "get_stats"):
                stats[name] = memory.get_stats()
        return stats

    def cleanup(self) -> None:
        for memory in (
            self.short_term,
            self.long_term,
            self.episodic,
            self.semantic,
            self.procedural,
        ):
            if memory is not None and hasattr(memory, "cleanup"):
                memory.cleanup()


def main() -> None:
    """CLI entrypoint. Wired up by ``python -m ...implementations``."""
    import argparse

    parser = argparse.ArgumentParser(description="HalalChain Memory Manager")
    parser.add_argument(
        "--action",
        choices=["store", "retrieve", "search", "consolidate", "stats"],
        required=True,
    )
    parser.add_argument("--key", help="Memory key")
    parser.add_argument("--value", help="Memory value (JSON)")
    parser.add_argument(
        "--type",
        default="short_term",
        choices=[t.value for t in MemoryType],
    )
    parser.add_argument("--query", help="Search query")
    parser.add_argument("--limit", type=int, default=10)
    args = parser.parse_args()

    manager = MemoryManager()

    if args.action == "store":
        if not args.key or not args.value:
            raise SystemExit("--key and --value required for store")
        value = json.loads(args.value)
        entry = manager.store(args.key, value, MemoryType(args.type))
        print(entry.id)
    elif args.action == "retrieve":
        if not args.key:
            raise SystemExit("--key required for retrieve")
        entry = manager.retrieve(args.key, MemoryType(args.type))
        if entry is None:
            print("Entry not found")
        else:
            print(json.dumps(entry.to_dict(), indent=2, default=str))
    elif args.action == "search":
        if not args.query:
            raise SystemExit("--query required for search")
        results = manager.search(args.query, MemoryType(args.type), args.limit)
        print(json.dumps(results, indent=2, default=str))
    elif args.action == "consolidate":
        print(json.dumps(manager.consolidate(), indent=2, default=str))
    elif args.action == "stats":
        print(json.dumps(manager.get_stats(), indent=2, default=str))


if __name__ == "__main__":
    main()
