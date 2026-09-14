"""Embedding helpers backed by a local sentence-transformers model.

Per the selected option we do NOT use a fake hash-based embedding.
The model is the same one the existing ``.halalchain/ai-inference``
service exposes via the gateway, but is loaded in-process here so
the memory layer can run without the gateway being up.

The ``sentence-transformers`` package is an optional dependency.
If it is not installed we raise a clear ``RuntimeError`` from
``get_embedding`` at call time rather than silently returning
meaningless vectors.
"""
from __future__ import annotations

import os
from functools import lru_cache
from typing import List


_MODEL_NAME = os.environ.get(
    "HALALCHAIN_EMBEDDING_MODEL",
    "sentence-transformers/all-MiniLM-L6-v2",
)
_EMBEDDING_DIM = 384


@lru_cache(maxsize=1)
def _get_model():
    try:
        from sentence_transformers import SentenceTransformer  # type: ignore
    except ImportError as exc:  # pragma: no cover - import guard
        raise RuntimeError(
            "sentence-transformers is required for HalalChain memory "
            "embeddings. Install with `pip install sentence-transformers` "
            "or set HALALCHAIN_EMBEDDING_BACKEND=disabled."
        ) from exc
    return SentenceTransformer(_MODEL_NAME)


def get_embedding(text: str) -> List[float]:
    """Return a 384-dim embedding for ``text`` using the local model."""
    model = _get_model()
    vector = model.encode(text, normalize_embeddings=True)
    return vector.tolist() if hasattr(vector, "tolist") else list(vector)


def get_embedding_dim() -> int:
    return _EMBEDDING_DIM
