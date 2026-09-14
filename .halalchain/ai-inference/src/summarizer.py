import re
from collections import Counter


def summarize(text: str, max_tokens: int = 80) -> str:
    """Extractive summarization using TF-IDF sentence scoring."""
    if not text.strip():
        return ""

    # Split into sentences
    sentences = re.split(r"[.!?]+s+", text.strip())
    sentences = [s.strip() for s in sentences if len(s.strip()) > 10]

    if not sentences:
        words = text.split()
        return " ".join(words[:max_tokens])

    # Word frequency scoring (TF)
    words = text.lower().split()
    freq = Counter(w for w in words if len(w) > 2)

    # Score each sentence
    scored = []
    for sent in sentences:
        sent_words = sent.lower().split()
        score = sum(freq.get(w, 0) for w in sent_words) / (len(sent_words) + 1)
        scored.append((sent, score))

    # Sort by score, then reassemble in original order up to max_tokens
    scored.sort(key=lambda x: x[1], reverse=True)

    result = []
    token_count = 0
    for sent, _ in scored:
        sent_tokens = len(sent.split())
        if token_count + sent_tokens <= max_tokens:
            result.append(sent)
            token_count += sent_tokens

    return " ".join(result) if result else " ".join(text.split()[:max_tokens])
