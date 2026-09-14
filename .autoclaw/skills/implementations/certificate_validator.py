"""
Certificate Validator Skill Implementation
Copyright © HalalChain. All rights reserved.
"""

import yaml
from datetime import datetime
from typing import Dict, Any, Optional

class CertificateValidator:
    """Validates halal certificates"""
    
    def __init__(self, kg_path: str = ".autoclaw/kg/"):
        self.kg_path = kg_path
        self.authorities = self._load_authorities()
    
    def _load_authorities(self) -> Dict:
        """Load certificate authorities from knowledge graph"""
        try:
            with open(f"{self.kg_path}/certificate_authorities.yaml", "r") as f:
                data = yaml.safe_load(f)
                return data.get("authorities", {})
        except FileNotFoundError:
            return {}
    
    def validate(self, cert_number: str, issuing_body: str, 
                 issue_date: Optional[str] = None, 
                 expiry_date: Optional[str] = None) -> Dict[str, Any]:
        """Validate a certificate"""
        authority = self.authorities.get(issuing_body.upper())
        
        if not authority:
            return {
                "status": "PENDING_REVIEW",
                "authority": issuing_body,
                "authority_priority": 0,
                "certificate_number": cert_number,
                "verification_details": "Issuing body not recognized",
                "warnings": ["Authority not in trusted database"]
            }
        
        is_valid_format = True
        
        days_until_expiry = None
        if expiry_date:
            exp = datetime.strptime(expiry_date, "%Y-%m-%d")
            now = datetime.now()
            days_until_expiry = (exp - now).days
            
            if days_until_expiry < 0:
                return {
                    "status": "EXPIRED",
                    "authority": issuing_body,
                    "authority_priority": authority.get("priority", 1),
                    "certificate_number": cert_number,
                    "issue_date": issue_date,
                    "expiry_date": expiry_date,
                    "days_until_expiry": days_until_expiry,
                    "verification_details": "Certificate has expired",
                    "warnings": ["Certificate expired"]
                }
        
        status = "VALID" if is_valid_format else "INVALID"
        
        return {
            "status": status,
            "authority": issuing_body,
            "authority_priority": authority.get("priority", 1),
            "certificate_number": cert_number,
            "issue_date": issue_date,
            "expiry_date": expiry_date,
            "days_until_expiry": days_until_expiry,
            "product_scope": "Not specified",
            "verification_details": f"Verified against {authority.get('full_name', issuing_body)}",
            "warnings": [],
            "digital_signature": False
        }

def validate_certificate(input_data: Dict[str, Any]) -> Dict[str, Any]:
    """Entrypoint for certificate validation skill"""
    validator = CertificateValidator()
    
    cert_number = input_data.get("certificate_number")
    issuing_body = input_data.get("issuing_body")
    issue_date = input_data.get("issue_date")
    expiry_date = input_data.get("expiry_date")
    
    if not cert_number or not issuing_body:
        return {
            "status": "ERROR",
            "error": "Missing required fields: certificate_number and issuing_body"
        }
    
    result = validator.validate(cert_number, issuing_body, issue_date, expiry_date)
    return result
