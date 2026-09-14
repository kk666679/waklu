from typing import Dict, List

HARAM_SIGNALS = ["pork", "lard", "gelatin", "alcohol", "ethanol", "carmine", "rennet", "pepsin", "blood", "cysteine"]
MASHBOOH_SIGNALS = ["emulsifier", "glycerin", "glycerol", "mono", "diglyceride", "flavour", "flavor", "e120", "e441", "e542"]
HALAL_SIGNALS = ["chicken", "beef", "lamb", "fish", "rice", "wheat", "sugar", "salt", "water", "milk", "honey", "coconut"]


def classify_halal(text: str, labels: List[str]) -> Dict[str, float]:
    """Classify text against halal/haram/mashbooh labels using multi-strategy scoring."""
    lower = text.lower()
    scores: Dict[str, float] = {}

    haram_hits = sum(1 for s in HARAM_SIGNALS if s in lower)
    mashbooh_hits = sum(1 for s in MASHBOOH_SIGNALS if s in lower)
    halal_hits = sum(1 for s in HALAL_SIGNALS if s in lower)

    # Base scores from keyword hits
    haram_score = min(0.95, 0.5 + haram_hits * 0.2) if haram_hits else 0.0
    mashbooh_score = min(0.85, 0.45 + mashbooh_hits * 0.15) if mashbooh_hits else 0.0
    halal_score = min(0.9, 0.3 + halal_hits * 0.1) if halal_hits else 0.3

    # If haram signals found, suppress halal and mashbooh
    if haram_hits:
        halal_score *= 0.1
        mashbooh_score *= 0.5
    elif mashbooh_hits:
        halal_score *= 0.6

    for label in labels:
        ll = label.lower()
        if ll == "haram": scores[label] = round(haram_score, 4)
        elif ll == "mashbooh": scores[label] = round(mashbooh_score, 4)
        elif ll == "halal": scores[label] = round(halal_score, 4)
        elif ll == "unknown": scores[label] = round(max(0.1, 1.0 - haram_score - mashbooh_score - halal_score), 4)
        else: scores[label] = 0.0

    return scores


def classify_generic(text: str, labels: List[str]) -> Dict[str, float]:
    """Keyword-based generic classification."""
    lower = text.lower()
    return {label: (1.0 if label.lower() in lower else 0.0) for label in labels}
