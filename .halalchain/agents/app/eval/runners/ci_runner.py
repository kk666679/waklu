"""CI runner for evaluation DAG - pytest-native, fails build on regression."""
from __future__ import annotations

import json
from pathlib import Path
from typing import Any
import pytest

from .trace_loader import TraceLoader
from .node_scorers import get_scorer
from .propagation import greedy_parent_attribution, EvalResult
from .taxonomy import FailureCategory


class CIRunner:
    """Runs evaluation DAG in CI mode - fails build on score regression."""

    def __init__(self, blob_store, goldens_dir: Path):
        self.loader = TraceLoader(blob_store)
        self.goldens_dir = goldens_dir
        self.thresholds = {
            "collector": 0.8,
            "classifier": 0.9,
            "verifier": 1.0,
            "handoff": 0.7,
            "gap": 0.7,
            "recollection": 1.0,
            "verdict": 1.0,
        }

    def load_goldens(self, workflow_name: str) -> dict:
        """Load golden expectations for a workflow."""
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
        """Evaluate a single trace against goldens."""
        trace = await self.loader.load(trace_id)
        goldens = self.load_goldens(workflow_name)

        node_scores = []
        for node in trace.nodes:
            scorer = get_scorer(node.get("type", "unknown"))
            score = await scorer.score(trace, node, goldens)
            node_scores.append(score)

        return greedy_parent_attribution(trace, node_scores)

    def assert_no_regression(self, results: list[EvalResult], workflow_name: str):
        """Assert no node scores below threshold - fails pytest on regression."""
        failures = []
        for r in results:
            if not r.passed:
                threshold = self.thresholds.get(r.node_type, 0.7)
                failures.append(
                    f"{workflow_name}/{r.node_id} ({r.node_type}): "
                    f"score={r.score:.2f} < threshold={threshold}, "
                    f"category={r.failure_category}, "
                    f"root_cause={r.is_root_cause}, "
                    f"propagated_from={r.propagated_from}"
                )

        if failures:
            pytest.fail(
                f"Evaluation regression in {workflow_name}:\n" + "\n".join(failures),
                pytrace=False,
            )


# Pytest fixtures and tests
@pytest.fixture
def ci_runner(blob_store, goldens_dir):
    return CIRunner(blob_store, goldens_dir)


@pytest.mark.asyncio
async def test_supplier_onboarding_eval(ci_runner):
    """Evaluate supplier onboarding workflow traces."""
    # Load recent traces for supplier_onboarding
    trace_ids = await ci_runner.loader.load_recent("supplier_onboarding", limit=10)
    for trace in trace_ids:
        results = await ci_runner.evaluate_trace(trace.trace_id, "supplier_onboarding")
        ci_runner.assert_no_regression(results, "supplier_onboarding")


@pytest.mark.asyncio
async def test_certificate_review_eval(ci_runner):
    """Evaluate certificate review workflow traces."""
    trace_ids = await ci_runner.loader.load_recent("certificate_review", limit=10)
    for trace in trace_ids:
        results = await ci_runner.evaluate_trace(trace.trace_id, "certificate_review")
        ci_runner.assert_no_regression(results, "certificate_review")