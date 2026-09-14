from src.core.models import EvidenceSignals, RiskSignals, ComplianceStatus
from src.policy import engine


def _evaluate(evidence, risk, missing=None, policy="MY-v3", jurisdiction="MY"):
    return engine.evaluate("PROD-001", evidence, risk, missing or [], policy, jurisdiction)


def test_verified():
    result = _evaluate(
        EvidenceSignals(ingredient_verification=0.9, supplier_verification=0.9,
                        certificate_verification=0.9, traceability=0.9, document_completeness=0.9),
        RiskSignals(overall_risk=0.1),
    )
    assert result.status == ComplianceStatus.VERIFIED
    assert not result.requires_human_review


def test_manual_review_low_ingredient():
    result = _evaluate(
        EvidenceSignals(ingredient_verification=0.3, supplier_verification=0.9,
                        certificate_verification=0.9, traceability=0.9, document_completeness=0.9),
        RiskSignals(overall_risk=0.2),
    )
    assert result.status == ComplianceStatus.MANUAL_REVIEW
    assert "INGREDIENT_SOURCE_UNVERIFIED" in result.reason_codes


def test_incomplete_missing_evidence():
    result = _evaluate(
        EvidenceSignals(), RiskSignals(),
        missing=["supplier_declaration"],
    )
    assert result.status == ComplianceStatus.INCOMPLETE
    assert result.requires_human_review


def test_hold_high_risk():
    result = _evaluate(
        EvidenceSignals(certificate_verification=0.9, ingredient_verification=0.9,
                        supplier_verification=0.9, traceability=0.9, document_completeness=0.9),
        RiskSignals(overall_risk=0.9),
    )
    assert result.status == ComplianceStatus.HOLD


def test_non_compliant_zero_certificate():
    result = _evaluate(
        EvidenceSignals(ingredient_verification=0.3, supplier_verification=0.3,
                        certificate_verification=0.0, traceability=0.5, document_completeness=0.5),
        RiskSignals(overall_risk=0.3),
    )
    assert result.status == ComplianceStatus.NON_COMPLIANT
