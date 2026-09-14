import re
from typing import List, Dict


def tokenize(text: str) -> List[str]:
    return re.findall(r"[a-z0-9]+", text.lower())


def score_rerank(query: str, passage: str) -> float:
    qt = tokenize(query)
    if not qt:
        return 0.0
    ps = set(tokenize(passage))
    matched = sum(1 for t in qt if t in ps)
    token_score = matched / len(qt)
    phrase_score = 0.2 if query.lower() in passage.lower() else 0.0
    return round(min(1.0, token_score * 0.7 + phrase_score * 0.3), 4)


def rerank(query: str, passages: List[str]) -> List[Dict]:
    results = [
        {"originalIndex": i, "passage": p, "score": score_rerank(query, p)}
        for i, p in enumerate(passages)
    ]
    results.sort(key=lambda x: x["score"], reverse=True)
    return results
