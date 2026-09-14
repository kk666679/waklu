"""Shared cache abstraction.

``ai-inference`` historically exposed an in-process LRU cache directly from
``src/cache.py``; ``tawheed`` does not use the same primitive. When a
service needs a cache that is materially the same (in-memory LRU with TTL,
optional promotion to Redis), they SHOULD use the helpers here instead of
defining their own.
"""

from __future__ import annotations

import time
from collections import OrderedDict
from typing import Any, Dict, Optional

__all__ = ["LRUCache", "build_cache"]


class LRUCache:
    """Tiny LRU cache with per-key TTL. Thread-unsafe — use one per worker."""

    def __init__(self, max_size: int = 10_000, ttl: int = 3600) -> None:
        self.max_size = max_size
        self.ttl = ttl
        self._cache: "OrderedDict[str, Any]" = OrderedDict()
        self._expiry: Dict[str, float] = {}
        self.hits = 0
        self.misses = 0

    def get(self, key: str) -> Optional[Any]:
        if key not in self._cache:
            self.misses += 1
            return None
        if time.time() > self._expiry.get(key, 0):
            self._cache.pop(key, None)
            self._expiry.pop(key, None)
            self.misses += 1
            return None
        self.hits += 1
        self._cache.move_to_end(key)
        return self._cache[key]

    def set(self, key: str, value: Any, ttl: Optional[int] = None) -> None:
        if key in self._cache:
            del self._cache[key]
        while len(self._cache) >= self.max_size:
            oldest = next(iter(self._cache))
            del self._cache[oldest]
            self._expiry.pop(oldest, None)
        self._cache[key] = value
        self._expiry[key] = time.time() + (ttl or self.ttl)

    def stats(self) -> Dict[str, Any]:
        total = self.hits + self.misses
        return {
            "size": len(self._cache),
            "hits": self.hits,
            "misses": self.misses,
            "hitRate": f"{(self.hits / total) * 100:.1f}%" if total else "N/A",
        }


def build_cache(*, strategy: str, max_size: int, ttl: int) -> Optional[LRUCache]:
    """Factory used by the services.

    * ``strategy == "lru"``  → return a fresh :class:`LRUCache`.
    * ``strategy == "redis"`` → return a fresh :class:`LRUCache` too. The
      services that need Redis-backed caching still wire the ``ai-inference``
      ``CacheService`` themselves; the shared package only standardizes
      the in-process fallback so both services behave identically when
      Redis is absent.
    * anything else / disabled → return ``None``.
    """
    if not strategy or strategy == "none":
        return None
    if strategy not in ("lru", "redis"):
        return None
    return LRUCache(max_size=max_size, ttl=ttl)
