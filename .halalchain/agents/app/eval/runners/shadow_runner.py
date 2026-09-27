"""Shadow runner for evaluation DAG - scores production traces on schedule."""
from __future__ import annotations

import asyncio
from datetime import datetime, timedelta
from pathlib import Path
from typing import Any
import json

from .trace_loader import TraceLoader
from .node_scorers import get_scorer
from .propagation import greedy_parent_attribution, EvalResult
from .taxonomy import FailureCategory


class ShadowRunner:
    """Scores production traces periodically, writes EvalResult to blob storage."""

    def __init__(
        self,
        blob_store,
        goldens_dir: Path,
        interval_minutes: int = 60,
        lookback_hours: int = 24,
    ):
        self.loader = TraceLoader(blob_store)
        self.goldens_dir = goldens_dir
        self.interval = interval_minutes * 60
        self.lookback = lookback_hours * 3600
        self._running = False

    def load_goldens(self, workflow_name: str) -> dict:
        golden_path = self.goldens_dir / f"{workflow_name}.jsonl"
        if not golden_path.exists():
            return {}
        goldens = {}
        with open(golden_path) as f:
            for line in f:
                if line.strip():
                    data = json.loads(line)
                    goldens[data["node_type"]] = data.get("expectations", {})
        return goldens

    async def evaluate_trace(self, trace_id: str, workflow_name: str) -> list[EvalResult]:
        trace = await self.loader.load(trace_id)
        goldens = self.load_goldens(workflow_name)

        node_scores = []
        for node in trace.nodes:
            scorer = get_scorer(node.get("type", "unknown"))
            score = await scorer.score(trace, node, goldens)
            node_scores.append(score)

        return greedy_parent_attribution(trace, node_scores)

    async def write_results(self, results: list[EvalResult], trace_id: str):
        """Write EvalResult to blob storage as EvidenceKind.AgentTrace."""
        for r in results:
            # EvalResult has: score, node, failure_category, root_cause - NO verdict field
            record = {
                "trace_id": r.trace_id,
                "node_id": r.node_id,
                "node_type": r.node_type,
                "score": r.score,
                "passed": r.passed,
                "failure_category": r.failure_category,
                "is_root_cause": r.is_root_cause,
                "propagated_from": r.propagated_from,
                "details": r.details,
                "evaluated_at": datetime.utcnow().isoformat(),
            }
            # Store as eval trace for observability
            key = f"eval/traces/{trace_id}/{r.node_id}.json"
            await self.blob_store.write(key, json.dumps(record).encode())

    async def run_once(self):
        """Evaluate all recent traces."""
        workflows = ["supplier_onboarding", "certificate_review", "product_verification"]
        for workflow in workflows:
            trace_ids = await self.loader.load_recent(workflow, limit=100)
            for trace in trace_ids:
                # Only evaluate traces within lookback window
                trace_time = datetime.fromisoformat(trace.started_at.replace("Z", "+00:00"))
                if (datetime.utcnow() - trace_time).total_seconds() > self.lookback:
                    continue

                results = await self.evaluate_trace(trace.trace_id, workflow)
                await self.write_results(results, trace.trace_id)

    async def start(self):
        """Run shadow evaluation loop."""
        self._running = True
        while self._running:
            try:
                await self.run_once()
            except Exception as e:
                # Log but don't crash - shadow mode must not affect production
                print(f"Shadow runner error: {e}")
            await asyncio.sleep(self.interval)

    def stop(self):
        self._running = False