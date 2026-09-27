"""HalalChain-specific failure taxonomy (21 categories)."""
from __future__ import annotations

from enum import Enum


class FailureCategory(str, Enum):
    """Standardized failure categories for agent evaluation."""

    # Collector failures
    COLLECTOR_MISSED_SOURCE = "collector.missed_source"
    COLLECTOR_TIMEOUT = "collector.timeout"
    COLLECTOR_AUTH_FAILED = "collector.auth_failed"
    COLLECTOR_RATE_LIMITED = "collector.rate_limited"

    # Classifier failures
    CLASSIFIER_MISLABEL = "classifier.mislabel"
    CLASSIFIER_LOW_CONFIDENCE = "classifier.low_confidence"
    CLASSIFIER_UNKNOWN_LABEL = "classifier.unknown_label"

    # Verifier failures
    VERIFIER_VALIDITY_DISAGREEMENT = "verifier.validity_disagreement"
    VERIFIER_EXPIRED_EVIDENCE = "verifier.expired_evidence"
    VERIFIER_MALFORMED_EVIDENCE = "verifier.malformed_evidence"

    # Handoff failures
    HANDOFF_CONTRACT_VIOLATION = "handoff.contract_violation"
    HANDOFF_MISSING_REQUIRED_FIELD = "handoff.missing_required_field"
    HANDOFF_SCHEMA_MISMATCH = "handoff.schema_mismatch"

    # Gap agent failures
    GAP_MISSED_REQUIREMENT = "gap.missed_requirement"
    GAP_FALSE_POSITIVE = "gap.false_positive"
    GAP_INCOMPLETE_REQUEST = "gap.incomplete_request"

    # Recollection failures
    RECOLLECTION_FAILED_TO_CLOSE = "recollection.failed_to_close"
    RECOLLECTION_MAX_RETRIES = "recollection.max_retries"

    # Verdict failures
    VERDICT_MISMATCH = "verdict.mismatch"

    # Infrastructure
    INFRA_BLOB_READ_FAILED = "infra.blob_read_failed"
    INFRA_LLM_UNAVAILABLE = "infra.llm_unavailable"

    @classmethod
    def collector_categories(cls) -> list["FailureCategory"]:
        return [cls.COLLECTOR_MISSED_SOURCE, cls.COLLECTOR_TIMEOUT, cls.COLLECTOR_AUTH_FAILED, cls.COLLECTOR_RATE_LIMITED]

    @classmethod
    def classifier_categories(cls) -> list["FailureCategory"]:
        return [cls.CLASSIFIER_MISLABEL, cls.CLASSIFIER_LOW_CONFIDENCE, cls.CLASSIFIER_UNKNOWN_LABEL]

    @classmethod
    def verifier_categories(cls) -> list["FailureCategory"]:
        return [cls.VERIFIER_VALIDITY_DISAGREEMENT, cls.VERIFIER_EXPIRED_EVIDENCE, cls.VERIFIER_MALFORMED_EVIDENCE]

    @classmethod
    def handoff_categories(cls) -> list["FailureCategory"]:
        return [cls.HANDOFF_CONTRACT_VIOLATION, cls.HANDOFF_MISSING_REQUIRED_FIELD, cls.HANDOFF_SCHEMA_MISMATCH]

    @classmethod
    def gap_categories(cls) -> list["FailureCategory"]:
        return [cls.GAP_MISSED_REQUIREMENT, cls.GAP_FALSE_POSITIVE, cls.GAP_INCOMPLETE_REQUEST]

    @classmethod
    def recollection_categories(cls) -> list["FailureCategory"]:
        return [cls.RECOLLECTION_FAILED_TO_CLOSE, cls.RECOLLECTION_MAX_RETRIES]

    @classmethod
    def verdict_categories(cls) -> list["FailureCategory"]:
        return [cls.VERDICT_MISMATCH]

    @classmethod
    def infra_categories(cls) -> list["FailureCategory"]:
        return [cls.INFRA_BLOB_READ_FAILED, cls.INFRA_LLM_UNAVAILABLE]

    @classmethod
    def all_categories(cls) -> list["FailureCategory"]:
        return list(cls)