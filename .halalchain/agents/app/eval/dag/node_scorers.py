"""Node scorers for evaluation DAG."""
from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass
from typing import Any
from deepeval.metrics import DAGMetric
from deepeval.test_case import LLMTestCase


@dataclass
class NodeScore:
    """Score for a single workflow node."""
    node_id: str
    node_type: str
    score: float  # 0.0 - 1.0
    passed: bool
    details: dict[str, Any]
    failure_category: Optional[str] = None


class NodeScorer(ABC):
    """Base class for scoring workflow nodes."""

    @abstractmethod
    async def score(self, trace, node: dict[str, Any], goldens: dict) -> NodeScore:
        """Score a single node."""
        pass


class CollectorScorer(NodeScorer):
    """Scores collector node: recall vs known-good sources."""

    async def score(self, trace, node: dict, goldens: dict) -> NodeScore:
        expected_sources = goldens.get("collector", {}).get("required_sources", [])
        actual_sources = node.get("output", {}).get("sources_found", [])

        if not expected_sources:
            return NodeScore(node["id"], "collector", 1.0, True, {"note": "no golden sources defined"})

        recall = len(set(expected_sources) & set(actual_sources)) / len(expected_sources)
        return NodeScore(
            node["id"],
            "collector",
            recall,
            recall >= 0.8,
            {"expected": expected_sources, "actual": actual_sources, "recall": recall},
            failure_category="collector.missed_source" if recall < 0.8 else None,
        )


class ClassifierScorer(NodeScorer):
    """Scores classifier node: type accuracy vs goldens."""

    async def score(self, trace, node: dict, goldens: dict) -> NodeScore:
        expected_labels = goldens.get("classifier", {}).get("expected_labels", {})
        actual_labels = node.get("output", {}).get("labels", {})

        if not expected_labels:
            return NodeScore(node["id"], "classifier", 1.0, True, {"note": "no golden labels defined"})

        correct = sum(1 for k, v in expected_labels.items() if actual_labels.get(k) == v)
        total = len(expected_labels)
        accuracy = correct / total if total else 1.0

        return NodeScore(
            node["id"],
            "classifier",
            accuracy,
            accuracy >= 0.9,
            {"expected": expected_labels, "actual": actual_labels, "accuracy": accuracy},
            failure_category="classifier.mislabel" if accuracy < 0.9 else None,
        )


class VerifierScorer(NodeScorer):
    """Scores verifier node: validity agreement."""

    async def score(self, trace, node: dict, goldens: dict) -> NodeScore:
        # Verifier checks if evidence is valid/well-formed
        expected_valid = goldens.get("verifier", {}).get("expected_valid", True)
        actual_valid = node.get("output", {}).get("is_valid", False)

        score = 1.0 if expected_valid == actual_valid else 0.0
        return NodeScore(
            node["id"],
            "verifier",
            score,
            score == 1.0,
            {"expected_valid": expected_valid, "actual_valid": actual_valid},
            failure_category="verifier.validity_disagreement" if score == 0.0 else None,
        )


class HandoffScorer(NodeScorer):
    """Scores tawheed handoff: bundle satisfies input contract (DAGMetric)."""

    def __init__(self):
        # DAGMetric for conditional rubric: if required field missing → 0, else evaluate quality
        self.metric = DAGMetric(
            name="tawheed_handoff_contract",
            dag="""
            REQUIRED_FIELDS = ["product_id", "certificate", "ingredients", "manufacturer"]
            if any field missing:
                score = 0
                reason = "missing_required_field"
            else:
                score = evaluate_quality(bundle)
                reason = "quality_score"
            """,
            threshold=0.7,
        )

    async def score(self, trace, node: dict, goldens: dict) -> NodeScore:
        bundle = node.get("output", {}).get("evidence_bundle", {})
        # The DAGMetric evaluates the bundle structure
        test_case = LLMTestCase(
            input="evidence_bundle",
            actual_output=json.dumps(bundle),
            expected_output=json.dumps(goldens.get("handoff", {}).get("expected_bundle", {})),
        )
        result = await self.metric.a_measure(test_case)

        return NodeScore(
            node["id"],
            "handoff",
            result.score,
            result.score >= 0.7,
            {"bundle": bundle, "reason": result.reason},
            failure_category="handoff.contract_violation" if result.score < 0.7 else None,
        )


class GapScorer(NodeScorer):
    """Scores gap agent: precision and recall of missing requirements."""

    async def score(self, trace, node: dict, goldens: dict) -> NodeScore:
        expected_missing = goldens.get("gap", {}).get("expected_missing", [])
        actual_missing = node.get("output", {}).get("missing_requirements", [])

        if not expected_missing:
            return NodeScore(node["id"], "gap", 1.0, True, {"note": "no missing requirements expected"})

        precision = len(set(expected_missing) & set(actual_missing)) / len(actual_missing) if actual_missing else 0.0
        recall = len(set(expected_missing) & set(actual_missing)) / len(expected_missing)
        f1 = 2 * precision * recall / (precision + recall) if (precision + recall) else 0.0

        return NodeScore(
            node["id"],
            "gap",
            f1,
            f1 >= 0.7,
            {"expected_missing": expected_missing, "actual_missing": actual_missing, "precision": precision, "recall": recall},
            failure_category="gap.missed_requirement" if f1 < 0.7 else None,
        )


class RecollectionScorer(NodeScorer):
    """Scores re-collection loop: did the loop close?"""

    async def score(self, trace, node: dict, goldens: dict) -> NodeScore:
        closed = node.get("output", {}).get("loop_closed", False)
        score = 1.0 if closed else 0.0
        return NodeScore(
            node["id"],
            "recollection",
            score,
            closed,
            {"loop_closed": closed},
            failure_category="recollection.failed_to_close" if not closed else None,
        )


class VerdictScorer(NodeScorer):
    """Scores terminal verdict: matches human label."""

    async def score(self, trace, node: dict, goldens: dict) -> NodeScore:
        expected_verdict = goldens.get("verdict", {}).get("expected_verdict")
        actual_verdict = node.get("output", {}).get("verdict")

        score = 1.0 if expected_verdict == actual_verdict else 0.0
        return NodeScore(
            node["id"],
            "verdict",
            score,
            score == 1.0,
            {"expected": expected_verdict, "actual": actual_verdict},
            failure_category="verdict.mismatch" if score == 0.0 else None,
        )


SCORERS: dict[str, type[NodeScorer]] = {
    "collector": CollectorScorer,
    "classifier": ClassifierScorer,
    "verifier": VerifierScorer,
    "handoff": HandoffScorer,
    "gap": GapScorer,
    "recollection": RecollectionScorer,
    "verdict": VerdictScorer,
}


def get_scorer(node_type: str) -> NodeScorer:
    """Get scorer instance for node type."""
    scorer_cls = SCORERS.get(node_type)
    if not scorer_cls:
        raise ValueError(f"No scorer for node type: {node_type}")
    return scorer_cls()