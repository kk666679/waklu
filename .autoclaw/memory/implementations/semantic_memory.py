"""Semantic memory: lightweight knowledge graph backed by SQLite.

Nodes / edges are stored as memory entries with type ``semantic`` and
a structured ``value`` JSON. The shared ``.autoclaw/vector/`` index
provides embedding search; graph traversal happens in Python.
"""
from __future__ import annotations

import json
import os
import sqlite3
from datetime import datetime
from typing import Any, Dict, List, Optional

from .embeddings import get_embedding
from .memory_manager import (
    MemoryEntry,
    MemoryStatus,
    MemoryType,
    _ensure_schema,
    _new_entry_id,
)
from .short_term_memory import _row_to_entry


class SemanticMemory:
    def __init__(self, config: Dict[str, Any]) -> None:
        self.config = config
        self.db_path = config.get("storage", {}).get(
            "database", ".autoclaw/memory/agent_memory.db"
        )
        self.embeddings_path = config.get("storage", {}).get(
            "embeddings", ".autoclaw/vector/embeddings.db"
        )
        self.relationships = set(
            config.get("graph", {}).get(
                "relationships",
                [
                    "is_a",
                    "part_of",
                    "derived_from",
                    "certified_by",
                    "contains",
                    "processed_by",
                    "classified_as",
                    "equivalent_to",
                ],
            )
        )
        self.max_depth = config.get("inference", {}).get("max_depth", 3)
        os.makedirs(os.path.dirname(self.embeddings_path) or ".", exist_ok=True)
        self.conn = sqlite3.connect(self.db_path, isolation_level=None)
        self.conn.row_factory = sqlite3.Row
        _ensure_schema(self.conn)
        self._ensure_edge_table()

    def _ensure_edge_table(self) -> None:
        self.conn.execute(
            """
            CREATE TABLE IF NOT EXISTS semantic_edges (
                id TEXT PRIMARY KEY,
                source_id TEXT,
                target_id TEXT,
                relationship TEXT,
                weight REAL,
                properties TEXT,
                created_at TEXT
            )
            """
        )

    def add_node(
        self,
        label: str,
        properties: Optional[Dict[str, Any]] = None,
        tags: Optional[List[str]] = None,
        source: str = "agent",
    ) -> MemoryEntry:
        now = datetime.now()
        value = {"label": label, "properties": properties or {}, "node": True}
        key = f"node:{label}:{now.timestamp()}"
        entry = MemoryEntry(
            id=_new_entry_id(key, "node"),
            key=key,
            value=value,
            type=MemoryType.SEMANTIC,
            importance=0.7,
            confidence=0.9,
            created_at=now,
            updated_at=now,
            ttl=0,
            access_count=0,
            last_accessed=now,
            source=source,
            tags=list(tags or []) + ["semantic", "node"],
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

    def add_edge(
        self,
        source_id: str,
        target_id: str,
        relationship: str,
        weight: float = 1.0,
        properties: Optional[Dict[str, Any]] = None,
    ) -> str:
        if self.relationships and relationship not in self.relationships:
            raise ValueError(
                f"Unknown relationship '{relationship}'. "
                f"Allowed: {sorted(self.relationships)}"
            )
        edge_id = _new_entry_id(f"{source_id}->{target_id}:{relationship}", "edge")
        self.conn.execute(
            """
            INSERT OR REPLACE INTO semantic_edges
            (id, source_id, target_id, relationship, weight, properties, created_at)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            """,
            (
                edge_id,
                source_id,
                target_id,
                relationship,
                weight,
                json.dumps(properties or {}),
                datetime.now().isoformat(),
            ),
        )
        return edge_id

    def store(self, key, value, ttl=None, importance=0.5, tags=None, source="agent"):
        node = self.add_node(
            label=str(value.get("label", key)),
            properties=value.get("properties", {}),
            tags=tags,
            source=source,
        )
        if isinstance(value, dict) and "edges" in value:
            for edge in value["edges"]:
                self.add_edge(
                    source_id=node.id,
                    target_id=edge["target_id"],
                    relationship=edge["relationship"],
                    weight=edge.get("weight", 1.0),
                    properties=edge.get("properties"),
                )
        return node

    def retrieve(self, key: str) -> Optional[MemoryEntry]:
        cur = self.conn.execute(
            "SELECT * FROM memory_entries WHERE key = ? AND type = 'semantic'",
            (key,),
        )
        row = cur.fetchone()
        return _row_to_entry(row) if row else None

    def search(self, query: str, limit: int = 10, min_score: float = 0.5):
        query_vec = get_embedding(query)
        cur = self.conn.execute(
            """
            SELECT e.*, v.embedding
            FROM memory_entries e
            LEFT JOIN vector_embeddings v ON e.id = v.entry_id
            WHERE e.type = 'semantic' AND e.status = 'active'
            """
        )
        results = []
        for row in cur.fetchall():
            entry = _row_to_entry(row)
            stored = json.loads(row["embedding"]) if row["embedding"] else []
            if not stored:
                continue
            score = self._cosine(query_vec, stored)
            if score >= min_score:
                results.append(
                    {
                        "entry": entry,
                        "similarity": score,
                        "relevance": score,
                        "importance": entry.importance,
                    }
                )
        results.sort(key=lambda r: r["relevance"], reverse=True)
        return results[:limit]

    def traverse(self, start_id: str, max_depth: Optional[int] = None) -> List[Dict[str, Any]]:
        depth = max_depth or self.max_depth
        seen = {start_id}
        frontier = [(start_id, 0)]
        results: List[Dict[str, Any]] = []
        while frontier:
            current, d = frontier.pop(0)
            if d >= depth:
                continue
            cur = self.conn.execute(
                "SELECT target_id, relationship, weight FROM semantic_edges WHERE source_id = ?",
                (current,),
            )
            for row in cur.fetchall():
                target = row["target_id"]
                results.append(
                    {
                        "from": current,
                        "to": target,
                        "relationship": row["relationship"],
                        "weight": row["weight"],
                        "depth": d + 1,
                    }
                )
                if target not in seen:
                    seen.add(target)
                    frontier.append((target, d + 1))
        return results

    @staticmethod
    def _cosine(a, b):
        if not a or not b:
            return 0.0
        n = min(len(a), len(b))
        a, b = a[:n], b[:n]
        dot = sum(x * y for x, y in zip(a, b))
        na = sum(x * x for x in a) ** 0.5
        nb = sum(y * y for y in b) ** 0.5
        return dot / (na * nb) if na and nb else 0.0

    def delete(self, key: str) -> bool:
        cur = self.conn.execute(
            "UPDATE memory_entries SET status = 'deleted' WHERE key = ? AND type = 'semantic'",
            (key,),
        )
        return cur.rowcount > 0

    def cleanup(self) -> None:
        return None

    def get_stats(self) -> Dict[str, Any]:
        cur = self.conn.execute("SELECT COUNT(*) AS c FROM memory_entries WHERE type='semantic'")
        nodes = cur.fetchone()["c"] or 0
        cur = self.conn.execute("SELECT COUNT(*) AS c FROM semantic_edges")
        edges = cur.fetchone()["c"] or 0
        return {"nodes": nodes, "edges": edges, "max_depth": self.max_depth}
