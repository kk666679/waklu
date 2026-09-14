"""
Jurisdiction Mapper Skill Implementation
Copyright © HalalChain. All rights reserved.
"""

import yaml
from typing import Dict, Any

def map_jurisdiction(input_data: Dict[str, Any]) -> Dict[str, Any]:
    """Map jurisdiction-specific halal requirements"""
    jurisdiction = input_data.get("jurisdiction")
    
    if not jurisdiction:
        return {"status": "ERROR", "error": "Missing required field: jurisdiction"}
    
    try:
        with open(".autoclaw/kg/jurisdiction_standards.yaml", "r") as f:
            data = yaml.safe_load(f) or {}
    except FileNotFoundError:
        data = {}
    
    jurisdictions = data.get("jurisdictions", {})
    entry = jurisdictions.get(jurisdiction, {})
    
    if not entry:
        return {
            "jurisdiction": jurisdiction,
            "authority": None,
            "standard": None,
            "requirements": [],
            "restrictions": [],
            "exceptions": [],
            "certification_required": False,
            "certification_bodies": [],
            "ingredient_restrictions": [],
            "additional_notes": "Jurisdiction not in database",
            "priority": 0,
            "compliance_required": False,
            "default_verdict": "MASHBOOH",
            "links": []
        }
    
    return {
        "jurisdiction": jurisdiction,
        "authority": entry.get("authority"),
        "standard": entry.get("standard"),
        "requirements": entry.get("requirements", []),
        "restrictions": entry.get("restrictions", []),
        "exceptions": entry.get("exceptions", []),
        "certification_required": entry.get("certification_required", False),
        "certification_bodies": entry.get("certification_bodies", []),
        "ingredient_restrictions": entry.get("ingredient_restrictions", []),
        "additional_notes": entry.get("additional_notes", ""),
        "priority": entry.get("priority", 1),
        "compliance_required": entry.get("compliance_required", False),
        "default_verdict": entry.get("default_verdict", "MASHBOOH"),
        "links": entry.get("links", [])
    }
