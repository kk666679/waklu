from __future__ import annotations
from typing import Any
from src.agents.base import BaseAgent, AgentEvidence
import logging

logger = logging.getLogger(__name__)

# Known haram ingredients (pork-derived, alcohol, etc.)
HARAM_KEYWORDS = {
    "pork", "lard", "gelatin", "porcine", "ham", "bacon", "pepperoni",
    "alcohol", "ethanol", "wine", "beer", "rum", "whiskey", "brandy",
    "non-halal", "haram", "khinzir",
}

# Known mashbooh (doubtful) ingredients
MASHBOOH_KEYWORDS = {
    "emulsifier", "e471", "e472", "e473", "e474", "e475", "e476",
    "natural flavors", "artificial flavors", "mono", "diglycerides",
    "stearic acid", "glycerin", "glycerol", "whey", "casein",
    "lecithin", "carmine", "cochineal", "shellac", "confectioners glaze",
}

# Known halal-safe E-codes
HALAL_ECODES = {
    "e100", "e101", "e120", "e140", "e160a", "e160b", "e160c",
    "e200", "e201", "e202", "e203", "e210", "e211", "e212", "e213",
    "e270", "e280", "e290", "e296", "e300", "e301", "e302", "e306",
    "e307", "e308", "e310", "e311", "e312", "e320", "e321", "e322",
    "e325", "e326", "e327", "e330", "e331", "e332", "e333", "e334",
    "e335", "e336", "e337", "e400", "e401", "e402", "e404", "e406",
    "e407", "e410", "e412", "e414", "e415", "e440", "e460", "e461",
    "e500", "e501", "e503", "e504", "e509", "e511", "e516", "e524",
}

# E-codes that are haram (pork-derived)
HARAM_ECODES = {
    "e441", "e920", "e921",
}


class IngredientAgent(BaseAgent):
    """Verifies ingredient lists for halal compliance.
    
    Uses keyword matching and E-code analysis to flag haram/mashbooh
    ingredients. In production, this agent would also call the ai-inference
    gateway for NLP-based ingredient classification.
    """

    name = "ingredient_agent"
    evidence_type = "ingredient"

    async def collect(self, product_id: str, context: dict[str, Any]) -> AgentEvidence:
        ingredients_text = context.get("ingredients", "")
        if not ingredients_text:
            return AgentEvidence(
                agent=self.name,
                evidence_type=self.evidence_type,
                confidence=0.0,
                source="no_data",
                details="No ingredient list provided",
                missing=["ingredient_list"],
            )

        ingredients = self._parse_ingredients(ingredients_text)
        haram_found = []
        mashbooh_found = []
        halal_found = []
        ecode_analysis = {}

        for ingredient in ingredients:
            name_lower = ingredient.lower().strip()
            
            # Check E-codes first
            ecode = self._extract_ecode(name_lower)
            if ecode:
                ecode_analysis[ecode] = self._classify_ecode(ecode)
                if ecode_analysis[ecode] == "haram":
                    haram_found.append(f"{ingredient} (E-code: {ecode})")
                    continue
                elif ecode_analysis[ecode] == "mashbooh":
                    mashbooh_found.append(f"{ingredient} (E-code: {ecode})")
                    continue

            # Check keyword lists
            if any(kw in name_lower for kw in HARAM_KEYWORDS):
                haram_found.append(ingredient)
            elif any(kw in name_lower for kw in MASHBOOH_KEYWORDS):
                mashbooh_found.append(ingredient)
            else:
                halal_found.append(ingredient)

        # Calculate confidence based on evidence quality
        total = len(ingredients)
        if total == 0:
            confidence = 0.0
        elif haram_found:
            confidence = 0.95  # High confidence when haram is found
        elif mashbooh_found:
            confidence = 0.6  # Medium confidence — needs human review
        else:
            confidence = min(0.85, 0.5 + (total * 0.02))  # Scales with completeness

        # Build signal scores
        ingredient_score = 1.0 if not haram_found else 0.0
        if mashbooh_found and not haram_found:
            ingredient_score = 0.5

        details_parts = []
        if haram_found:
            details_parts.append(f"HARAM: {', '.join(haram_found)}")
        if mashbooh_found:
            details_parts.append(f"MASHBOOH: {', '.join(mashbooh_found)}")
        details_parts.append(f"HALAL: {len(halal_found)} ingredients verified")

        return AgentEvidence(
            agent=self.name,
            evidence_type=self.evidence_type,
            confidence=confidence,
            signals={"ingredient_score": ingredient_score},
            source="ingredient_analysis",
            details=" | ".join(details_parts),
            missing=[],
        )

    def _parse_ingredients(self, text: str) -> list[str]:
        """Parse comma-separated ingredient list."""
        import re
        # Split on commas, semicolons, or newlines
        parts = re.split(r"[,;\n]+", text)
        return [p.strip() for p in parts if p.strip()]

    def _extract_ecode(self, text: str) -> str | None:
        """Extract E-code from ingredient text."""
        import re
        m = re.search(r"\b[eE](\d{3}[a-z]?)\b", text)
        return f"e{m.group(1).lower()}" if m else None

    def _classify_ecode(self, ecode: str) -> str:
        """Classify an E-code as halal, haram, or mashbooh."""
        if ecode in HARAM_ECODES:
            return "haram"
        if ecode in HALAL_ECODES:
            return "halal"
        return "mashbooh"  # Unknown E-code is mashbooh
