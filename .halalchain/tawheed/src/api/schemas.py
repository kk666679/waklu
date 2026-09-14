from __future__ import annotations
from pydantic import BaseModel
from src.core.models import VerificationResult


class VerifyProductRequest(BaseModel):
    jurisdiction: str = "MY"
    policy_version: str = "MY-v3"
    document_text: str | None = None
    # Agent context fields
    certificate_number: str | None = None
    certification_body: str | None = None
    expiry_date: str | None = None
    issue_date: str | None = None
    scope: str | None = None
    ingredients: str | None = None
    supplier_name: str | None = None
    supplier_country: str | None = None
    supplier_status: str | None = None
    vendor_country: str | None = None
    documents: list[str] | None = None


class VerifyProductResponse(BaseModel):
    result: VerificationResult


class HealthResponse(BaseModel):
    status: str
    service: str
    demo_mode: bool
    policy_version: str
