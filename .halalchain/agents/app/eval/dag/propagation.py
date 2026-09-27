"""Failure propagation and root cause attribution for evaluation DAG."""
from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Optional
from .node_scorers import NodeScore


@dataclass
class EvalResult:
    """Result of evaluating a single node in the DAG."""
    trace_id: str
    node_id: str
    node_type: str
    score: float
    passed: bool
    failure_category: Optional[str]
    is_root_cause: bool
    propagated_from: Optional[str] = None
    details: dict[str, Any] = None


def greedy_parent_attribution(
    trace,
    node_scores: list[NodeScore],
) -> list[EvalResult]:
    """
    Apply greedy-parent strategy to find root causes.

    Algorithm (from AgentEval paper):
    1. Score all nodes independently
    2. For each failed node, walk backward through DAG edges
    3. First failed ancestor = root cause; downstream = propagated
    4. Nodes with no failed ancestors are independent root causes
    """
    # Build adjacency for backward walk
    node_map = {n["id"]: n for n in trace.nodes}
    edges = trace.edges  # list of (from_id, to_id)
    children = {n["id"]: [] for n in trace.nodes}
    parents = {n["id"]: [] for n in trace.nodes}
    for from_id, to_id in edges:
        children[from_id].append(to_id)
        parents[to_id].append(from_id)

    score_map = {ns.node_id: ns for ns in node_scores}
    results = []

    for ns in node_scores:
        if ns.passed:
            results.append(EvalResult(
                trace_id=trace.trace_id,
                node_id=ns.node_id,
                node_type=ns.node_type,
                score=ns.score,
                passed=True,
                failure_category=None,
                is_root_cause=False,
            ))
            continue

        # Walk backward to find first failed ancestor
        root_cause = None
        visited = set()

        def walk_back(node_id: str) -> Optional[str]:
            if node_id in visited:
                return None
            visited.add(node_id)
            parent_ids = parents.get(node_id, [])
            for pid in parent_ids:
                pscore = score_map.get(pid)
                if pscore and not pscore.passed:
                    return pid
                # Recurse
                found = walk_back(pid)
                if found:
                    return found
            return None

        root_cause = walk_back(ns.node_id)

        if root_cause == ns.node_id:
            is_root = True
            propagated_from = None
        elif root_cause:
            is_root = False
            propagated_from = root_cause
        else:
            # No failed ancestors — this is an independent root cause
            is_root = True
            propagated_from = None

        results.append(EvalResult(
            trace_id=trace.trace_id,
            node_id=ns.node_id,
            node_type=ns.node_type,
            score=ns.score,
            passed=False,
            failure_category=ns.failure_category,
            is_root_cause=is_root,
            propagated_from=propagated_from,
            details=ns.details,
        ))

    return results


def aggregate_taxonomy(eval_results: list[EvalResult]) -> dict[str, int]:
    """Aggregate failure categories across results."""
    counts = {}
    for r in eval_results:
        if not r.passed and r.failure_category:
            counts[r.failure_category] = counts.get(r.failure_category, 0) + 1
    return counts