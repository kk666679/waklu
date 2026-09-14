import re
from typing import List, Dict, Optional

HARAM_KEYWORDS = ["pork", "lard", "gelatin", "alcohol", "ethanol", "carmine", "rennet", "pepsin", "blood", "cysteine"]
MASHBOOH_KEYWORDS = ["emulsifier", "glycerin", "glycerol", "mono", "diglyceride", "flavour", "flavor", "e120", "e441", "e542"]
E_CODES = {"e120": "haram", "e441": "haram", "e542": "mashbooh", "e471": "mashbooh", "e621": "mashbooh", "e627": "halal", "e631": "halal"}


def assess_risk(text: str) -> str:
    lower = text.lower()
    if any(s in lower for s in HARAM_KEYWORDS):
        return "haram"
    if any(s in lower for s in MASHBOOH_KEYWORDS):
        return "mashbooh"
    return "halal"


def parse_ingredients(text: str) -> List[Dict]:
    raw = [item.strip() for item in re.split(r"[,;]\s*", text) if item.strip()]
    results = []
    for item in raw:
        pct = re.search(r"(\d+(?:\.\d+)?)\s*%", item)
        ec = re.search(r"\b(e\d{3,4}[a-z]?)\b", item, re.IGNORECASE)
        name = re.sub(r"\(.*?\)", "", item)
        name = re.sub(r"\d+(?:\.\d+)?%?", "", name)
        name = re.sub(r"\s+", " ", name).strip()

        risk = assess_risk(item)
        if ec:
            code_lower = ec.group(1).lower()
            if code_lower in E_CODES:
                risk = E_CODES[code_lower]

        results.append({
            "name": name,
            "percentage": f"{pct.group(1)}%" if pct else None,
            "eCode": ec.group(1).upper() if ec else None,
            "risk": risk,
        })
    return results


def parse_and_summarize(text: str) -> Dict:
    parsed = parse_ingredients(text)
    return {
        "parsed": parsed,
        "summary": {
            "total": len(parsed),
            "halal": sum(1 for i in parsed if i["risk"] == "halal"),
            "mashbooh": sum(1 for i in parsed if i["risk"] == "mashbooh"),
            "haram": sum(1 for i in parsed if i["risk"] == "haram"),
        },
    }
