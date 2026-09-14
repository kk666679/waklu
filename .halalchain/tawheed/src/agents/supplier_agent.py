from __future__ import annotations
from typing import Any
from src.agents.base import BaseAgent, AgentEvidence
import logging

logger = logging.getLogger(__name__)

# Countries with well-established halal supply chains
KNOWN_HALAL_COUNTRIES = {"MY", "ID", "SG", "BN", "AE", "SA", "QA", "BH", "KW", "OM"}
# Countries requiring extra scrutiny
REVIEW_COUNTRIES = {"CN", "IN", "TH", "VN", "PH"}


class SupplierAgent(BaseAgent):
    """Verifies supplier information for halal supply chain integrity.
    
    Checks supplier country, registration status, and cross-references
    against known halal supply chain data.
    """

    name = "supplier_agent"
    evidence_type = "supplier"

    async def collect(self, product_id: str, context: dict[str, Any]) -> AgentEvidence:
        supplier_name = context.get("supplier_name", "")
        supplier_country = context.get("supplier_country", "")
        supplier_status = context.get("supplier_status", "")
        vendor_country = context.get("vendor_country", "")

        if not supplier_name and not vendor_country:
            return AgentEvidence(
                agent=self.name,
                evidence_type=self.evidence_type,
                confidence=0.0,
                source="no_data",
                details="No supplier information provided",
                missing=["supplier_name", "supplier_country"],
            )

        checks = []
        confidence_factors = []
        missing = []

        # Check 1: Supplier name
        if supplier_name:
            checks.append(f"supplier_name:provided:{supplier_name}")
            confidence_factors.append(0.6)
        else:
            missing.append("supplier_name")

        # Check 2: Supplier country
        country = supplier_country or vendor_country
        if country:
            country_upper = country.upper().strip()
            if country_upper in KNOWN_HALAL_COUNTRIES:
                checks.append(f"supplier_country:halal_established:{country_upper}")
                confidence_factors.append(0.9)
            elif country_upper in REVIEW_COUNTRIES:
                checks.append(f"supplier_country:needs_review:{country_upper}")
                confidence_factors.append(0.5)
            else:
                checks.append(f"supplier_country:unknown:{country_upper}")
                confidence_factors.append(0.4)
        else:
            missing.append("supplier_country")

        # Check 3: Supplier status
        if supplier_status:
            if supplier_status.lower() in ("active", "verified", "approved"):
                checks.append(f"supplier_status:active")
                confidence_factors.append(0.8)
            elif supplier_status.lower() == "pending":
                checks.append("supplier_status:pending")
                confidence_factors.append(0.3)
            else:
                checks.append(f"supplier_status:{supplier_status}")
                confidence_factors.append(0.4)
        else:
            missing.append("supplier_status")

        # Calculate confidence
        confidence = sum(confidence_factors) / len(confidence_factors) if confidence_factors else 0.0
        supplier_score = confidence

        return AgentEvidence(
            agent=self.name,
            evidence_type=self.evidence_type,
            confidence=confidence,
            signals={"supplier_score": supplier_score},
            source="supplier_verification",
            details=f"Checks: {'; '.join(checks)}",
            missing=missing,
        )
