"""
Supply Chain Analyzer Skill Implementation
Copyright © HalalChain. All rights reserved.
"""

import yaml
from typing import Dict, Any, Optional

class SupplyChainAnalyzer:
    """Analyzes supply chain for halal compliance risks"""
    
    def __init__(self, kg_path: str = ".autoclaw/kg/"):
        self.kg_path = kg_path
        self.rules = self._load_rules()
    
    def _load_rules(self) -> Dict:
        """Load proprietary rules from knowledge graph"""
        try:
            with open(f"{self.kg_path}/proprietary_rules.yaml", "r") as f:
                return yaml.safe_load(f) or {}
        except FileNotFoundError:
            return {}
    
    def analyze(self, supplier: str, product: str,
                certification: Optional[str] = None,
                country: Optional[str] = None,
                handling_procedures: Optional[str] = None) -> Dict[str, Any]:
        """Analyze supply chain compliance"""
        concerns = []
        recommendations = []
        risk_score = 0
        
        certification_status = "VERIFIED" if certification else "UNVERIFIED"
        if not certification:
            concerns.append("No halal certification provided")
            recommendations.append("Request valid halal certification from supplier")
            risk_score += 30
        
        traceability = "PARTIAL"
        if certification and country:
            traceability = "FULL"
        elif not certification and not country:
            traceability = "NONE"
            risk_score += 25
        
        handling_risk = "LOW"
        if handling_procedures:
            if any(kw in handling_procedures.lower() for kw in ["pork", "alcohol", "non-halal"]):
                handling_risk = "HIGH"
                concerns.append("Handling procedures indicate non-halal cross-contamination risk")
                risk_score += 40
            elif "shared" in handling_procedures.lower():
                handling_risk = "MEDIUM"
                concerns.append("Shared equipment or facilities detected")
                risk_score += 20
        
        contamination_risk = handling_risk
        
        if risk_score >= 50:
            status = "NON_COMPLIANT"
        elif risk_score >= 25:
            status = "RISK_IDENTIFIED"
        else:
            status = "COMPLIANT"
        
        if not recommendations:
            recommendations.append("Maintain current supplier verification processes")
        
        return {
            "status": status,
            "supplier": supplier,
            "certification_status": certification_status,
            "traceability": traceability,
            "handling_risk": handling_risk,
            "contamination_risk": contamination_risk,
            "concerns": concerns,
            "recommendations": recommendations,
            "risk_score": min(risk_score, 100),
            "audit_required": risk_score >= 40
        }

def analyze_supply_chain(input_data: Dict[str, Any]) -> Dict[str, Any]:
    """Entrypoint for supply chain analysis skill"""
    analyzer = SupplyChainAnalyzer()
    
    supplier = input_data.get("supplier")
    product = input_data.get("product")
    
    if not supplier or not product:
        return {
            "status": "ERROR",
            "error": "Missing required fields: supplier and product"
        }
    
    return analyzer.analyze(
        supplier=supplier,
        product=product,
        certification=input_data.get("certification"),
        country=input_data.get("country"),
        handling_procedures=input_data.get("handling_procedures")
    )
