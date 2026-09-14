from __future__ import annotations
from typing import Any
from src.agents.base import BaseAgent, AgentEvidence
from datetime import datetime, timezone
import logging

logger = logging.getLogger(__name__)

# Known certification bodies and their jurisdictions
CERT_BODIES = {
    "JAKIM": {"country": "MY", "reliability": 0.95},
    "MUI": {"country": "ID", "reliability": 0.90},
    "BPJPH": {"country": "ID", "reliability": 0.88},
    "MUIS": {"country": "SG", "reliability": 0.92},
    "ESMA": {"country": "AE", "reliability": 0.93},
    "GSO": {"country": "SA", "reliability": 0.91},
    "IFANCA": {"country": "US", "reliability": 0.85},
    "HFA": {"country": "AU", "reliability": 0.85},
}


class CertificateAgent(BaseAgent):
    """Verifies halal certificates for authenticity and validity.
    
    Checks certificate number format, issuer, expiry, and scope.
    In production, this agent would query certifying body databases
    or use OCR on uploaded certificate images.
    """

    name = "certificate_agent"
    evidence_type = "certificate"

    async def collect(self, product_id: str, context: dict[str, Any]) -> AgentEvidence:
        cert_number = context.get("certificate_number", "")
        cert_body = context.get("certification_body", "")
        expiry_str = context.get("expiry_date", "")
        jurisdiction = context.get("jurisdiction", "")
        issue_date_str = context.get("issue_date", "")
        scope = context.get("scope", "")

        if not cert_number and not cert_body:
            return AgentEvidence(
                agent=self.name,
                evidence_type=self.evidence_type,
                confidence=0.0,
                source="no_data",
                details="No certificate information provided",
                missing=["certificate_number", "certification_body"],
            )

        checks = []
        confidence_factors = []
        missing = []

        # Check 1: Certificate number format
        if cert_number:
            if len(cert_number) >= 6:
                checks.append("certificate_number:format_valid")
                confidence_factors.append(0.8)
            else:
                checks.append("certificate_number:format_invalid")
                confidence_factors.append(0.2)
        else:
            missing.append("certificate_number")

        # Check 2: Certification body
        if cert_body:
            body_upper = cert_body.upper().strip()
            if body_upper in CERT_BODIES:
                info = CERT_BODIES[body_upper]
                checks.append(f"certification_body:known:{body_upper}")
                confidence_factors.append(info["reliability"])
                if not jurisdiction:
                    jurisdiction = info["country"]
            else:
                checks.append(f"certification_body:unknown:{cert_body}")
                confidence_factors.append(0.4)
        else:
            missing.append("certification_body")

        # Check 3: Expiry date
        expiry = self._parse_date(expiry_str)
        if expiry:
            now = datetime.now(timezone.utc)
            days_until_expiry = (expiry - now).days
            if days_until_expiry > 90:
                checks.append(f"expiry:valid:{days_until_expiry}d_remaining")
                confidence_factors.append(0.9)
            elif days_until_expiry > 30:
                checks.append(f"expiry:expiring_soon:{days_until_expiry}d_remaining")
                confidence_factors.append(0.6)
            elif days_until_expiry > 0:
                checks.append(f"expiry:expiring:{days_until_expiry}d_remaining")
                confidence_factors.append(0.3)
            else:
                checks.append("expiry:expired")
                confidence_factors.append(0.0)
        else:
            missing.append("expiry_date")

        # Check 4: Scope
        if scope:
            checks.append("scope:provided")
            confidence_factors.append(0.7)
        else:
            missing.append("scope")

        # Check 5: Issue date
        issue_date = self._parse_date(issue_date_str)
        if issue_date:
            checks.append("issue_date:provided")
            confidence_factors.append(0.6)
        else:
            missing.append("issue_date")

        # Calculate overall confidence
        confidence = sum(confidence_factors) / len(confidence_factors) if confidence_factors else 0.0

        # Certificate score for evidence signals
        cert_score = confidence

        return AgentEvidence(
            agent=self.name,
            evidence_type=self.evidence_type,
            confidence=confidence,
            signals={"certificate_score": cert_score},
            source="certificate_verification",
            details=f"Checks: {'; '.join(checks)}",
            missing=missing,
        )

    def _parse_date(self, date_str: str) -> datetime | None:
        """Parse date string in various formats."""
        if not date_str:
            return None
        for fmt in ["%Y-%m-%d", "%d/%m/%Y", "%m/%d/%Y", "%Y-%m-%dT%H:%M:%S", "%d-%m-%Y"]:
            try:
                return datetime.strptime(date_str, fmt).replace(tzinfo=timezone.utc)
            except ValueError:
                continue
        return None
