from __future__ import annotations
from abc import ABC, abstractmethod
from typing import Any
from src.core.models import EvidenceSignals
import logging

logger = logging.getLogger(__name__)


class BaseAgent(ABC):
    """Base class for all evidence collection agents.
    
    Each agent specializes in collecting one type of evidence for the
    halal verification workflow. Agents collect evidence; the Policy Engine
    makes the compliance decision.
    """

    name: str = "base"
    evidence_type: str = "unknown"

    @abstractmethod
    async def collect(self, product_id: str, context: dict[str, Any]) -> AgentEvidence:
        """Collect evidence for a product. Returns structured evidence."""
        ...

    async def health_check(self) -> bool:
        """Check if the agent is operational."""
        return True


class AgentEvidence:
    """Structured evidence collected by an agent."""

    def __init__(
        self,
        agent: str,
        evidence_type: str,
        confidence: float,
        signals: dict[str, float] | None = None,
        source: str = "",
        details: str = "",
        missing: list[str] | None = None,
    ):
        self.agent = agent
        self.evidence_type = evidence_type
        self.confidence = confidence
        self.signals = signals or {}
        self.source = source
        self.details = details
        self.missing = missing or []

    def to_dict(self) -> dict:
        return {
            "agent": self.agent,
            "evidence_type": self.evidence_type,
            "confidence": self.confidence,
            "signals": self.signals,
            "source": self.source,
            "details": self.details,
            "missing": self.missing,
        }
