"""
Ingredient Verifier Skill Implementation
Copyright © HalalChain. All rights reserved.
"""

import yaml
import re
from typing import Dict, Any, Optional

class IngredientVerifier:
    """Verifies halal status of ingredients"""
    
    def __init__(self, kg_path: str = ".autoclaw/kg/"):
        self.kg_path = kg_path
        self.e_codes = self._load_e_codes()
        self.ingredients = self._load_ingredients()
    
    def _load_e_codes(self) -> Dict:
        """Load E-code classifications from knowledge graph"""
        try:
            with open(f"{self.kg_path}/halal_e_codes.yaml", "r") as f:
                data = yaml.safe_load(f)
                return data.get("codes", {})
        except FileNotFoundError:
            return {}
    
    def _load_ingredients(self) -> Dict:
        """Load ingredient classifications from knowledge graph"""
        try:
            with open(f"{self.kg_path}/ingredient_db.yaml", "r") as f:
                data = yaml.safe_load(f)
                return data.get("ingredients", {})
        except FileNotFoundError:
            return {}
    
    def _normalize_ingredient(self, ingredient: str) -> str:
        """Normalize ingredient name for lookup"""
        return ingredient.lower().strip().replace(" ", "_")
    
    def _extract_e_code(self, ingredient: str) -> Optional[str]:
        """Extract E-code from ingredient string"""
        pattern = r"E[0-9]{3,4}"
        match = re.search(pattern, ingredient, re.IGNORECASE)
        return match.group(0).upper() if match else None
    
    def verify(self, ingredient: str, source: Optional[str] = None) -> Dict[str, Any]:
        """Verify ingredient halal status"""
        normalized = self._normalize_ingredient(ingredient)
        e_code = self._extract_e_code(ingredient)
        
        if e_code and e_code in self.e_codes:
            entry = self.e_codes[e_code]
            return {
                "status": entry.get("status", "UNKNOWN"),
                "confidence": 1.0 if entry.get("status") != "UNKNOWN" else 0.5,
                "classification": entry.get("name", ""),
                "notes": entry.get("notes", ""),
                "references": entry.get("references", []),
                "e_code": e_code
            }
        
        if normalized in self.ingredients:
            entry = self.ingredients[normalized]
            return {
                "status": entry.get("status", "UNKNOWN"),
                "confidence": 0.9,
                "classification": ingredient,
                "notes": entry.get("notes", ""),
                "references": [],
                "e_code": None
            }
        
        return {
            "status": "UNKNOWN",
            "confidence": 0.3,
            "classification": ingredient,
            "notes": "Ingredient not found in database. Manual review required.",
            "references": [],
            "e_code": None
        }

def verify_ingredient(input_data: Dict[str, Any]) -> Dict[str, Any]:
    """Entrypoint for ingredient verification skill"""
    verifier = IngredientVerifier()
    ingredient = input_data.get("ingredient")
    source = input_data.get("source")
    
    if not ingredient:
        return {
            "status": "ERROR",
            "error": "Missing required field: ingredient"
        }
    
    result = verifier.verify(ingredient, source)
    return result
