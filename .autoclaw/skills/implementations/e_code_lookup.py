"""
E-Code Lookup Skill Implementation
Copyright © HalalChain. All rights reserved.
"""

import yaml
import re
from typing import Dict, Any

def lookup_e_code(input_data: Dict[str, Any]) -> Dict[str, Any]:
    """Look up halal status of an E-code"""
    e_code = input_data.get("e_code")
    include_sources = input_data.get("include_sources", True)
    
    if not e_code:
        return {"status": "ERROR", "error": "Missing required field: e_code"}
    
    normalized = re.sub(r"[^0-9]", "", str(e_code).upper())
    canonical = f"E{normalized}" if normalized else str(e_code).upper()
    
    try:
        with open(".autoclaw/kg/halal_e_codes.yaml", "r") as f:
            data = yaml.safe_load(f) or {}
    except FileNotFoundError:
        data = {}
    
    codes = data.get("codes", {})
    entry = codes.get(canonical, {})
    
    if not entry:
        return {
            "e_code": canonical,
            "name": None,
            "status": "UNKNOWN",
            "confidence": 0.0,
            "source": None,
            "notes": "E-code not in database",
            "references": [],
            "alternative_names": [],
            "category": None,
            "last_updated": None,
            "similar_codes": []
        }
    
    return {
        "e_code": canonical,
        "name": entry.get("name"),
        "status": entry.get("status", "UNKNOWN"),
        "confidence": entry.get("confidence", 0.8) if entry.get("status") else 0.0,
        "source": entry.get("source") if include_sources else None,
        "notes": entry.get("notes", ""),
        "references": entry.get("references", []),
        "alternative_names": entry.get("alternative_names", []),
        "category": entry.get("category"),
        "jurisdiction_notes": entry.get("jurisdiction_notes", {}),
        "last_updated": entry.get("last_updated"),
        "similar_codes": entry.get("similar_codes", [])
    }
