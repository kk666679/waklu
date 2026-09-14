"""Tests for the Tawheed orchestrator's failure semantics.

A complete agent failure must NOT be converted into a high-risk
"everything is fine" verdict. It must raise so the policy engine can
emit a deterministic "evidence unavailable" result.
"""

import os
import pytest

os.environ.setdefault("DEMO_MODE", "true")
os.environ.setdefault("JWT_SECRET", "test-secret-key-that-is-at-least-32-chars-long!!")

from src.agents.base import BaseAgent, AgentEvidence
from src.agents.orchestrator import AgentOrchestrator, AllAgentsFailed


class _AlwaysFails(BaseAgent):
    name = "always-fails"
    async def collect(self, product_id, context):
        raise RuntimeError("boom")


class _AlwaysSucceeds(BaseAgent):
    name = "always-succeeds"
    async def collect(self, product_id, context):
        return AgentEvidence(
            agent=self.name,
            evidence_type="test",
            signals={"ingredient_verification": 0.9},
            missing=[],
            confidence=0.9,
        )


@pytest.mark.asyncio
async def test_all_agents_fail_raises_AllAgentsFailed(monkeypatch):
    orch = AgentOrchestrator.__new__(AgentOrchestrator)
    class FailA(BaseAgent):
        name = "fail-a"
        async def collect(self, product_id, context):
            raise RuntimeError("boom")
    class FailB(BaseAgent):
        name = "fail-b"
        async def collect(self, product_id, context):
            raise RuntimeError("boom")
    orch.agents = [FailA(), FailB()]
    with pytest.raises(AllAgentsFailed) as excinfo:
        await orch.collect_evidence("PID-1", {})
    assert excinfo.value.product_id == "PID-1"
    assert set(excinfo.value.errors) == {"fail-a", "fail-b"}


@pytest.mark.asyncio
async def test_partial_failure_preserves_errors(monkeypatch):
    orch = AgentOrchestrator.__new__(AgentOrchestrator)
    orch.agents = [_AlwaysSucceeds(), _AlwaysFails()]
    evidence, risk, missing, agent_evidences, errors = await orch.collect_evidence("PID-2", {})
    assert set(errors.keys()) == {"always-fails"}
    assert "boom" in errors["always-fails"]
    assert len(agent_evidences) == 1
    # Risk must reflect the partial failure.
    assert 0.0 < risk.overall_risk <= 1.0


@pytest.mark.asyncio
async def test_no_failures_returns_empty_errors_dict():
    orch = AgentOrchestrator.__new__(AgentOrchestrator)
    orch.agents = [_AlwaysSucceeds()]
    evidence, risk, missing, agent_evidences, errors = await orch.collect_evidence("PID-3", {})
    assert errors == {}
    assert risk.overall_risk < 0.5  # 0.7 * 0.1 + 0.3 * 0 = 0.07
