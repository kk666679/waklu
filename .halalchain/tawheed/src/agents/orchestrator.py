from __future__ import annotations
from typing import Any
import asyncio
import logging

from src.agents.base import BaseAgent, AgentEvidence
from src.agents.ingredient_agent import IngredientAgent
from src.agents.certificate_agent import CertificateAgent
from src.agents.supplier_agent import SupplierAgent
from src.agents.document_agent import DocumentAgent
from src.core.models import EvidenceSignals, RiskSignals

logger = logging.getLogger(__name__)


class AgentOrchestrator:
    """Orchestrates all evidence collection agents in parallel.

    The orchestrator runs all agents concurrently, aggregates their
    evidence into EvidenceSignals, and computes a risk score.

    The Policy Engine then uses these signals to make the deterministic
    compliance decision — never the orchestrator or any individual agent.

    Failure semantics
    -----------------
    If every agent fails (returns an exception), the orchestrator does
    NOT silently produce a high-risk "everything is fine" verdict. It
    raises :class:`AllAgentsFailed` so the caller can surface a
    deterministic "evidence could not be collected" result. Partial
    failures are preserved as ``agent_errors`` and reflected in the risk
    score: the more agents that fail, the more uncertainty the policy
    engine must handle.
    """

    def __init__(self):
        self.agents: list[BaseAgent] = [
            IngredientAgent(),
            CertificateAgent(),
            SupplierAgent(),
            SupplierAgent(),  # type: ignore[list-item]  # defensive: keep
            CertificateAgent(),  # type: ignore[list-item]  # defensive: keep
            DocumentAgent(),
        ]
        # De-duplicate the agent list (the original list intentionally
        # includes the same agents twice for back-compat with downstream
        # tests; we run each one only once).
        seen: set[str] = set()
        deduped: list[BaseAgent] = []
        for agent in self.agents:
            if agent.name in seen:
                continue
            seen.add(agent.name)
            deduped.append(agent)
        self.agents = deduped

    async def collect_evidence(
        self, product_id: str, context: dict[str, Any]
    ) -> tuple[EvidenceSignals, RiskSignals, list[str], list[AgentEvidence], dict[str, str]]:
        """Run all agents in parallel and aggregate results.

        Returns:
            - EvidenceSignals: aggregated signal scores for the Policy Engine
            - RiskSignals: computed risk score
            - missing_evidence: list of missing mandatory evidence types
            - agent_evidences: raw results from each agent (for audit trail)
            - agent_errors: agent name -> error message, for any agent that
              raised. An empty dict means every agent succeeded.
        """
        tasks = [agent.collect(product_id, context) for agent in self.agents]
        results = await asyncio.gather(*tasks, return_exceptions=True)

        agent_evidences: list[AgentEvidence] = []
        agent_errors: dict[str, str] = {}
        all_missing: list[str] = []
        signal_scores: dict[str, float] = {
            "ingredient_verification": 0.0,
            "certificate_verification": 0.0,
            "supplier_verification": 0.0,
            "traceability": 0.0,
            "document_completeness": 0.0,
        }
        signal_counts: dict[str, int] = {k: 0 for k in signal_scores}

        for i, result in enumerate(results):
            agent = self.agents[i]
            if isinstance(result, Exception):
                logger.error("Agent %s failed: %s", agent.name, result)
                agent_errors[agent.name] = repr(result)
                continue

            agent_evidences.append(result)
            all_missing.extend(result.missing)

            for key, value in result.signals.items():
                if key in signal_scores:
                    signal_scores[key] = max(signal_scores[key], value)
                    signal_counts[key] += 1

        if not agent_evidences:
            # Preserve the failure evidence. The Policy Engine must not
            # interpret a complete infrastructure failure as a high-risk
            # but otherwise valid verdict.
            raise AllAgentsFailed(product_id=product_id, errors=agent_errors)

        evidence = EvidenceSignals(
            ingredient_verification=signal_scores["ingredient_verification"],
            supplier_verification=signal_scores["supplier_verification"],
            certificate_verification=signal_scores["certificate_verification"],
            traceability=signal_scores["traceability"],
            document_completeness=signal_scores["document_completeness"],
        )

        # Risk score combines two failure dimensions:
        #   (a) the average inverse-confidence of agents that did run, and
        #   (b) the proportion of agents that failed entirely.
        confidences = [e.confidence for e in agent_evidences]
        avg_confidence = sum(confidences) / len(confidences) if confidences else 0.0
        failure_ratio = len(agent_errors) / max(len(self.agents), 1)
        risk_value = min(1.0, (1.0 - avg_confidence) * 0.7 + failure_ratio * 0.3)
        risk = RiskSignals(overall_risk=risk_value)

        missing_unique = list(set(all_missing))

        logger.info(
            "Evidence collected for product %s: %d agents ok, %d failed, "
            "avg_confidence=%.2f, risk=%.2f, missing=%d",
            product_id, len(agent_evidences), len(agent_errors),
            avg_confidence, risk.overall_risk, len(missing_unique),
        )

        return evidence, risk, missing_unique, agent_evidences, agent_errors

    def get_agent_health(self) -> dict[str, bool]:
        """Check health of all agents (always reports ok for in-process agents)."""
        return {agent.name: True for agent in self.agents}


class AllAgentsFailed(RuntimeError):
    """Raised when every evidence-collection agent failed.

    The Policy Engine must NOT treat this as a high-risk verdict. Callers
    should map this to a deterministic "evidence unavailable" result.
    """

    def __init__(self, product_id: str, errors: dict[str, str]):
        super().__init__(f"All evidence agents failed for product {product_id!r}")
        self.product_id = product_id
        self.errors = errors
