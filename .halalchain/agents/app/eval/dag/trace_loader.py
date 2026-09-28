"""Agent trace loader from blob storage.

Reads traces produced by ``runtime.dag.WorkflowRunner``. The runner and this
loader are one contract: the runner writes ``edges`` as ``[from, to]`` pairs
because every consumer here iterates them as 2-tuples. A dict-shaped edge
would unpack to the literal strings ``"from"`` and ``"to"`` and silently
corrupt root-cause attribution, so ``_normalize_edges`` accepts both forms but
the runner emits pairs.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Optional, Protocol
import json
import logging

logger = logging.getLogger(__name__)


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


class BlobStore(Protocol):
    """The blob-store surface the loader needs.

    ``list`` is required by :meth:`TraceLoader.load_recent`. It is optional on
    purpose: a deployment can score a known set of trace ids via
    :meth:`load` and :meth:`load_batch` without granting the eval harness
    directory enumeration, which is a wider capability than it needs.
    """

    async def read(self, key: str) -> bytes: ...

    async def list(self, prefix: str) -> list[str]: ...


def _normalize_edges(raw: Any) -> list[tuple[str, str]]:
    """Coerce persisted edges into 2-tuples.

    Accepts ``[from, to]`` pairs (what the runner writes) and
    ``{"from": ..., "to": ...}`` dicts (what an earlier trace format wrote),
    because a dict of exactly two keys would otherwise unpack to the strings
    ``"from"`` and ``"to"`` — silently wrong rather than loudly broken.
    """
    edges: list[tuple[str, str]] = []
    for edge in raw or []:
        if isinstance(edge, dict):
            source = edge.get("from")
            target = edge.get("to")
            if source is None or target is None:
                continue
            edges.append((str(source), str(target)))
        elif isinstance(edge, (list, tuple)) and len(edge) == 2:
            edges.append((str(edge[0]), str(edge[1])))
    return edges


class TraceLoader:
    """Loads AgentTrace objects from blob storage.

    Key layout is ``traces/{workflow}/{trace_id}.json``. It has to be
    consistent across every method here: the runners call ``load`` and
    ``load_recent`` against the same store, and a loader whose two entry
    points disagree on where a trace lives reads nothing while appearing to
    work.
    """

    def __init__(self, blob_store, *, on_error: Any = None):
        self.blob_store = blob_store
        # Called with (trace_id, exception) for traces that could not be
        # loaded. A skip is not silent: an unreadable trace is a gap in the
        # evidence, and a shadow pass that quietly scores 90 of 100 traces
        # is indistinguishable from one that scored all 100.
        self._on_error = on_error

    @staticmethod
    def trace_key(workflow_name: str, trace_id: str) -> str:
        return f"traces/{workflow_name}/{trace_id}.json"

    async def load(self, trace_id: str, workflow_name: str) -> AgentTrace:
        """Load a single trace by ID within a workflow."""
        data = await self.blob_store.read(self.trace_key(workflow_name, trace_id))
        payload = json.loads(data)
        payload["edges"] = _normalize_edges(payload.get("edges"))
        return AgentTrace(**payload)

    async def load_batch(
        self, workflow_name: str, trace_ids: list[str]
    ) -> list[AgentTrace]:
        """Load multiple traces, skipping any that cannot be read."""
        loaded: list[AgentTrace] = []
        for trace_id in trace_ids:
            try:
                loaded.append(await self.load(trace_id, workflow_name))
            except Exception as exc:  # noqa: BLE001 - one bad trace must not abort the batch
                self._report(trace_id, exc)
        return loaded

    async def load_recent(self, workflow_name: str, limit: int = 100) -> list[AgentTrace]:
        """Load the most recent traces for a workflow, newest first.

        This is what the shadow runner schedules over, so while it raised
        ``NotImplementedError`` the entire shadow evaluation path was dead on
        arrival — it looked implemented and had never run.

        Ordering is by ``started_at`` rather than by key order: blob stores
        have no meaningful listing order, and "recent" has to mean recent
        rather than whatever the backend happened to return.
        """
        list_prefix = getattr(self.blob_store, "list", None)
        if list_prefix is None:
            raise TypeError(
                f"{type(self.blob_store).__name__} does not support listing, so "
                f"load_recent('{workflow_name}') cannot enumerate traces. Either "
                "give the store a list(prefix) method, or drive the shadow "
                "runner with explicit trace ids via load_batch()."
            )

        keys = await list_prefix(f"traces/{workflow_name}/")
        prefix = f"traces/{workflow_name}/"

        loaded: list[AgentTrace] = []
        for key in keys:
            if not key.endswith(".json"):
                continue
            trace_id = key[len(prefix):][: -len(".json")]
            try:
                # Read the key we were given rather than reconstructing it:
                # reconstructing assumes one layout and silently returns
                # nothing when the store uses another.
                data = await self.blob_store.read(key)
                payload = json.loads(data)
                payload["edges"] = _normalize_edges(payload.get("edges"))
                loaded.append(AgentTrace(**payload))
            except Exception as exc:  # noqa: BLE001 - one bad trace must not abort the pass
                self._report(trace_id, exc)

        loaded.sort(key=lambda t: t.started_at, reverse=True)
        return loaded[:limit]

    def _report(self, trace_id: str, exc: Exception) -> None:
        if self._on_error is not None:
            self._on_error(trace_id, exc)
        else:
            logger.warning("Skipping unreadable trace %s: %s", trace_id, exc)


class InMemoryBlobStore:
    """Minimal blob store for tests and local shadow runs.

    Implements the optional ``list`` capability so ``load_recent`` is
    exercisable without a real backend.
    """

    def __init__(self) -> None:
        self._objects: dict[str, bytes] = {}

    async def write(self, key: str, data: bytes) -> None:
        self._objects[key] = data

    async def read(self, key: str) -> bytes:
        return self._objects[key]

    async def list(self, prefix: str) -> list[str]:
        return [k for k in self._objects if k.startswith(prefix)]
