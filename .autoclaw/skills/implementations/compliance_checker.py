"""
Compliance Checker Skill Implementation
Copyright © HalalChain. All rights reserved.
"""

import yaml
import json
from datetime import datetime
from typing import Dict, Any, List

class ComplianceChecker:
    """Performs comprehensive compliance check on product data"""
    
    def __init__(self, kg_path: str = ".autoclaw/kg/"):
        self.kg_path = kg_path
        self.e_codes = self._load_yaml("halal_e_codes.yaml")
        self.authorities = self._load_yaml("certificate_authorities.yaml")
        self.jurisdictions = self._load_yaml("jurisdiction_standards.yaml")
        self.rules = self._load_yaml("proprietary_rules.yaml")
    
    def _load_yaml(self, filename: str) -> Dict:
        try:
            with open(f"{self.kg_path}/{filename}", "r") as f:
                return yaml.safe_load(f) or {}
        except FileNotFoundError:
            return {}
    
    def check(self, product_data: Dict[str, Any]) -> Dict[str, Any]:
        """Run comprehensive compliance check"""
        issues = []
        recommendations = []
        missing = []
        score = 100
        
        ingredients = product_data.get("ingredients", [])
        if not ingredients:
            missing.append("ingredients")
            issues.append("No ingredients provided")
            score -= 30
        
        certificates = product_data.get("certificates", [])
        if not certificates:
            missing.append("certificates")
            issues.append("No certificates provided")
            score -= 20
        
        jurisdiction = product_data.get("jurisdiction")
        if not jurisdiction:
            missing.append("jurisdiction")
            score -= 10
        
        haram_ingredients = [i for i in ingredients if isinstance(i, str) and 
                            any(h in i.lower() for h in ["pork", "alcohol", "lard"])]
        if haram_ingredients:
            issues.append(f"Potentially haram ingredients detected: {haram_ingredients}")
            score -= 50
        
        ingredient_check = {
            "total": len(ingredients),
            "haram_detected": len(haram_ingredients),
            "status": "FAIL" if haram_ingredients else "PASS"
        }
        
        certificate_check = {
            "total": len(certificates),
            "valid": len([c for c in certificates if isinstance(c, dict) and c.get("status") == "VALID"]),
            "status": "PASS" if certificates else "PENDING"
        }
        
        supply_chain_check = {
            "supplier_provided": bool(product_data.get("supplier")),
            "status": "PASS" if product_data.get("supplier") else "PENDING"
        }
        
        jurisdiction_check = {
            "jurisdiction": jurisdiction,
            "applies": jurisdiction in (self.jurisdictions.get("jurisdictions", {}) if isinstance(self.jurisdictions.get("jurisdictions"), dict) else {}),
            "status": "PASS" if jurisdiction else "PENDING"
        }
        
        if haram_ingredients:
            overall_status = "FAIL"
            verdict = "HARAM"
        elif score < 70:
            overall_status = "REVIEW"
            verdict = "MASHBOOH"
        elif missing:
            overall_status = "PENDING"
            verdict = "MASHBOOH"
        else:
            overall_status = "PASS"
            verdict = "HALAL"
        
        if overall_status != "PASS":
            recommendations.append("Provide missing information and re-verify")
        if haram_ingredients:
            recommendations.append("Remove haram ingredients or verify source")
        
        return {
            "overall_status": overall_status,
            "verdict": verdict,
            "confidence": max(0.0, min(1.0, score / 100)),
            "ingredient_check": ingredient_check,
            "certificate_check": certificate_check,
            "supply_chain_check": supply_chain_check,
            "jurisdiction_check": jurisdiction_check,
            "issues": issues,
            "recommendations": recommendations,
            "overall_score": max(0, score),
            "review_needed": overall_status in ("FAIL", "REVIEW"),
            "missing_information": missing
        }

def check_compliance(input_data: Dict[str, Any]) -> Dict[str, Any]:
    """Entrypoint for compliance check skill"""
    checker = ComplianceChecker()
    product_data = input_data.get("product_data")
    
    if not product_data:
        return {"status": "ERROR", "error": "Missing required field: product_data"}
    
    return checker.check(product_data)
