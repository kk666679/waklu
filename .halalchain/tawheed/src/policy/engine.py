from __future__ import annotations
from src.core.models import ComplianceStatus, EvidenceSignals, RiskSignals, VerificationResult
from src.core.config import get_settings
from src.observability import _fingerprint, tawheed_verdicts_total, measure_verdict

POLICY_VERSIONS = {
    "MY-v3": {"min_cert": 0.8, "min_ingredient": 0.7, "min_supplier": 0.6, "max_risk": 0.65},
    "ID-v1": {"min_cert": 0.8, "min_ingredient": 0.7, "min_supplier": 0.6, "max_risk": 0.65},
}


def evaluate(
    product_id: str,
    evidence: EvidenceSignals,
    risk: RiskSignals,
    missing_evidence: list[str],
    policy_version: str,
    jurisdiction: str,
) -> VerificationResult:
    """
    Deterministic policy evaluation.
    Inputs are evidence signals collected by AI agents.
    The compliance decision is made here — never by an LLM.
    """
    settings = get_settings()
    rules = POLICY_VERSIONS.get(policy_version, POLICY_VERSIONS["MY-v3"])
    reason_codes: list[str] = []

    with measure_verdict("evaluating"):
        if missing_evidence:
            result = VerificationResult(
                product_id=product_id, status=ComplianceStatus.INCOMPLETE,
                verification=evidence, risk=risk,
                missing_evidence=missing_evidence, reason_codes=["MISSING_MANDATORY_EVIDENCE"],
                policy_version=policy_version, jurisdiction=jurisdiction,
                requires_human_review=True,
            )
            _fingerprint(result, result)
            tawheed_verdicts_total.labels(verdict="INCOMPLETE").inc()
            return result

        if risk.overall_risk >= settings.risk_threshold_hold:
            result = VerificationResult(
                product_id=product_id, status=ComplianceStatus.HOLD,
                verification=evidence, risk=risk,
                missing_evidence=[], reason_codes=["HIGH_RISK_SCORE"],
                policy_version=policy_version, jurisdiction=jurisdiction,
                requires_human_review=True,
            )
            _fingerprint(result, result)
            tawheed_verdicts_total.labels(verdict="HOLD").inc()
            return result

        if evidence.certificate_verification < rules["min_cert"]:
            reason_codes.append("CERTIFICATE_INSUFFICIENT")
        if evidence.ingredient_verification < rules["min_ingredient"]:
            reason_codes.append("INGREDIENT_SOURCE_UNVERIFIED")
        if evidence.supplier_verification < rules["min_supplier"]:
            reason_codes.append("SUPPLIER_UNVERIFIED")

        if reason_codes:
            status = (ComplianceStatus.NON_COMPLIANT
                      if evidence.certificate_verification == 0
                      else ComplianceStatus.MANUAL_REVIEW)
            result = VerificationResult(
                product_id=product_id, status=status,
                verification=evidence, risk=risk,
                missing_evidence=[], reason_codes=reason_codes,
                policy_version=policy_version, jurisdiction=jurisdiction,
                requires_human_review=True,
            )
            _fingerprint(result, result)
            tawheed_verdicts_total.labels(verdict=status.value).inc()
            return result

        result = VerificationResult(
            product_id=product_id, status=ComplianceStatus.VERIFIED,
            verification=evidence, risk=risk,
            missing_evidence=[], reason_codes=[],
            policy_version=policy_version, jurisdiction=jurisdiction,
            requires_human_review=False,
        )
        _fingerprint(result, result)
        tawheed_verdicts_total.labels(verdict="VERIFIED").inc()
        return result
