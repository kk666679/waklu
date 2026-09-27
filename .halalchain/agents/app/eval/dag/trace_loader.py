"""Agent trace loader from blob storage."""
from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Optional
import json


@dataclass
class AgentTrace:
    """Normalized agent execution trace."""
    trace_id: str
    workflow_name: str
    nodes: list[dict[str, Any]]
    edges: list[tuple[str, str]]
    metadata: dict[str, Any]
    started_at: str
    completed_at: Optional[str]
    status: str  # "success" | "failure" | "partial"


class TraceLoader:
    """Loads AgentTrace objects from blob storage."""

    def __init__(self, blob_store):
        self.blob_store = blob_store

    async def load(self, trace_id: str) -> AgentTrace:
        """Load a single trace by ID."""
        data = await self.blob_store.read(f"traces/{trace_id}.json")
        return AgentTrace(**json.loads(data))

    async def load_batch(self, trace_ids: list[str]) -> list[AgentTrace]:
        """Load multiple traces."""
        return [await self.load(tid) for tid in trace_ids]

    async def load_recent(self, workflow_name: str, limit: int = 100) -> list[AgentTrace]:
        """Load recent traces for a workflow."""
        # Implementation depends on blob store listing capabilities
        raise NotImplementedError