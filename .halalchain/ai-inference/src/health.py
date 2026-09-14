import time
from typing import Dict, Any

started_at = time.time()
request_count = 0


def get_health(cache_stats: Dict[str, Any]) -> Dict[str, Any]:
    return {
        "service": "halalchain-ai-inference",
        "status": "healthy",
        "utcNow": __import__("datetime").datetime.utcnow().isoformat() + "Z",
        "uptime": int(time.time() - started_at),
        "requestCount": request_count,
        "cache": cache_stats,
    }
