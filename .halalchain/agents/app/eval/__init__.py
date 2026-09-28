"""Evaluation package for HalalChain agents.

The DAG half (trace loading, scorers, propagation, taxonomy) is stdlib-only
and imports eagerly. The LLM-rubric metrics and the runners that depend on
them are resolved lazily through ``__getattr__``, because they require the
``eval`` extra (deepeval) — which is a CI and shadow-image dependency, not a
runtime one.

Without this split, ``from eval.dag.trace_loader import TraceLoader`` failed
on a deployment that had no deepeval installed, even though nothing in that
import path uses it. The shadow runner was therefore unreachable in any
environment where it could actually be scheduled.
"""
from __future__ import annotations

__version__ = "0.1.0"

from typing import Any

from .dag.trace_loader import InMemoryBlobStore, TraceLoader, AgentTrace
from .dag.node_scorers import get_scorer, NodeScorer, NodeScore
from .dag.propagation import greedy_parent_attribution, EvalResult
from .dag.taxonomy import FailureCategory

#: Names resolved on first access, mapped to the module that provides them.
_LAZY = {
    "handoff_contract_metric": ".metrics.handoff_contract",
    "explanation_faithfulness_metric": ".metrics.explanation_faithfulness",
    "CIRunner": ".runners.ci_runner",
    "ShadowRunner": ".runners.shadow_runner",
}


def __getattr__(name: str) -> Any:
    module_path = _LAZY.get(name)
    if module_path is None:
        raise AttributeError(f"module {__name__!r} has no attribute {name!r}")

    from importlib import import_module

    try:
        module = import_module(module_path, __name__)
    except ModuleNotFoundError as exc:
        raise ModuleNotFoundError(
            f"{name} requires the 'eval' extra (deepeval): pip install "
            f"'halalchain-agents[eval]'. The deterministic DAG scorers and the "
            "trace loader do not need it."
        ) from exc

    return getattr(module, name)


def __dir__() -> list[str]:
    return sorted(__all__)


__all__ = [
    "TraceLoader",
    "InMemoryBlobStore",
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
