"""Evaluation package for HalalChain agents."""
from __future__ import annotations

__version__ = "0.1.0"

from .dag.trace_loader import TraceLoader, AgentTrace
from .dag.node_scorers import get_scorer, NodeScorer, NodeScore
from .dag.propagation import greedy_parent_attribution, EvalResult
from .dag.taxonomy import FailureCategory
from .metrics.handoff_contract import handoff_contract_metric
from .metrics.explanation_faithfulness import explanation_faithfulness_metric
from .runners.ci_runner import CIRunner
from .runners.shadow_runner import ShadowRunner

__all__ = [
    "TraceLoader",
    "AgentTrace",
    "get_scorer",
    "NodeScorer",
    "NodeScore",
    "greedy_parent_attribution",
    "EvalResult",
    "FailureCategory",
    "handoff_contract_metric",
    "explanation_faithfulness_metric",
    "CIRunner",
    "ShadowRunner",
]