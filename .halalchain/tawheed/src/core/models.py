from __future__ import annotations
from enum import StrEnum
from pydantic import BaseModel, Field
from datetime import datetime, timezone


class ComplianceStatus(StrEnum):
    """
    Deterministic output of the Policy Engine.
    AI agents collect evidence; this enum is never set by an LLM.
    """
    VERIFIED = "VERIFIED"
    MANUAL_REVIEW = "MANUAL_REVIEW"
    INCOMPLETE = "INCOMPLETE"
    HOLD = "HOLD"
    NON_COMPLIANT = "NON_COMPLIANT"
    UNVERIFIED = "UNVERIFIED"


class EvidenceSignals(BaseModel):
    ingredient_verification: float = 0.0
    supplier_verification: float = 0.0
    certificate_verification: float = 0.0
    traceability: float = 0.0
    document_completeness: float = 0.0


class RiskSignals(BaseModel):
    overall_risk: float = 0.0


class VerificationResult(BaseModel):
    product_id: str
    status: ComplianceStatus
    verification: EvidenceSignals
    risk: RiskSignals
    missing_evidence: list[str] = Field(default_factory=list)
    reason_codes: list[str] = Field(default_factory=list)
    policy_version: str
    jurisdiction: str
    requires_human_review: bool
    verified_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))

    def model_dump(self, **kwargs):
        d = super().model_dump(**kwargs)
        d["verified_at"] = self.verified_at.isoformat()
        return d
