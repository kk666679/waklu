"""Structural invariant: the LLM never assigns the halal verdict.

The halalChain platform's architectural principle is that the LLM
generates evidence and explanation only; the deterministic policy engine
owns ``ComplianceStatus``. This test enforces the invariant structurally
on the Python side: there is no LLM-generated text that can be cast to
a ``ComplianceStatus`` enum value without going through the policy
engine.
"""

import os
import pytest

os.environ.setdefault("DEMO_MODE", "true")
os.environ.setdefault("JWT_SECRET", "test-secret-key-that-is-at-least-32-chars-long!!")

from src.core.models import ComplianceStatus
from src.policy import engine


def test_llm_text_cannot_become_a_compliance_status():
    # A "verdict-shaped" string from an LLM must not be interpretable
    # as a real ComplianceStatus value.
    for text in ("HALAL", "HARAM", "MASHBOOH", "halal", "Halal", "halal.\n"):
        try:
            ComplianceStatus(text)
        except ValueError:
            # The text is not a valid enum value — good.
            continue
        # If a text ever silently maps to a status, the invariant is broken.
        pytest.fail(f"LLM verdict text {text!r} was accepted as a ComplianceStatus")


def test_policy_engine_is_the_only_path_to_compliance_status():
    # The policy engine's evaluate() returns a structured result. There
    # is no public helper in the codebase that derives a
    # ComplianceStatus from raw LLM output.
    import inspect
    from src.policy import engine as policy_engine
    src = inspect.getsource(policy_engine)
    # The policy engine must reference ComplianceStatus at least once
    # (i.e. it is the one that produces it).
    assert "ComplianceStatus" in src
    # The LLM client modules must NOT import ComplianceStatus.
    from src.agents import orchestrator
    orch_src = inspect.getsource(orchestrator)
    assert "ComplianceStatus" not in orch_src, (
        "AgentOrchestrator must not import ComplianceStatus — that would "
        "let the LLM-driven orchestrator influence the final verdict."
    )


def test_evaluate_returns_known_status_only():
    from src.core.models import EvidenceSignals, RiskSignals
    known = {c.value for c in ComplianceStatus}
    result = engine.evaluate(
        "PROD-1",
        EvidenceSignals(),
        RiskSignals(),
        ["supplier_declaration"],
        "MY-v3",
        "MY",
    )
    assert result.status.value in known
