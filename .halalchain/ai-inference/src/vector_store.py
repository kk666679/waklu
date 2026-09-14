from typing import List, Dict, Any, Optional
import logging
from .config import settings

logger = logging.getLogger(__name__)


class VectorStore:
    """Vector store interface for RAG."""
    async def initialize(self):
        pass

    async def add_documents(self, documents: List[Dict[str, Any]]):
        raise NotImplementedError

    async def search(self, query_embedding: List[float], top_k: int = 5) -> List[Dict[str, Any]]:
        raise NotImplementedError

    async def clear(self):
        pass

    async def get_stats(self) -> Dict[str, Any]:
        return {"provider": "none", "enabled": False}


class QdrantVectorStore(VectorStore):
    """Qdrant vector database."""

    def __init__(self):
        self.client = None
        self.collection = settings.qdrant_collection
        self._healthy: bool = False

    async def initialize(self):
        try:
            from qdrant_client import QdrantClient
            from qdrant_client.http.models import Distance, VectorParams
            # Use the URL form so the operator can point at either a host:port
            # pair (default qdrant_host/qdrant_port) or a full URL. When the
            # service runs in docker compose, ``qdrant_host`` should be set
            # to the Compose service hostname (NOT localhost).
            if str(settings.qdrant_host).startswith(("http://", "https://")):
                self.client = QdrantClient(url=settings.qdrant_host)
            else:
                self.client = QdrantClient(
                    host=settings.qdrant_host, port=settings.qdrant_port
                )
            collections = self.client.get_collections().collections
            exists = any(c.name == self.collection for c in collections)
            if not exists:
                self.client.create_collection(
                    collection_name=self.collection,
                    vectors_config=VectorParams(size=settings.embedding_dim, distance=Distance.COSINE),
                )
                logger.info(f"Created Qdrant collection: {self.collection}")
            self._healthy = True
            logger.info(f"Qdrant connected: {settings.qdrant_host}:{settings.qdrant_port}")
        except Exception as e:
            # We surface the failure rather than silently returning an
            # empty RAG result. /health/ready will mark the service as
            # degraded, and callers can decide to fall back.
            self._healthy = False
            self.client = None
            logger.error(
                f"Qdrant init failed (host={settings.qdrant_host}:{settings.qdrant_port}): {e}"
            )

    async def add_documents(self, documents: List[Dict[str, Any]]):
        if not self.client:
            raise RuntimeError("Vector store is not initialized; cannot add documents")
        from qdrant_client.http.models import PointStruct
        points = [
            PointStruct(id=doc["id"], vector=doc["embedding"], payload={"content": doc["content"], **doc.get("metadata", {})})
            for doc in documents if "embedding" in doc
        ]
        if points:
            self.client.upsert(collection_name=self.collection, points=points)

    async def search(self, query_embedding: List[float], top_k: int = 5) -> List[Dict[str, Any]]:
        if not self.client:
            raise RuntimeError("Vector store is not initialized; cannot search")
        results = self.client.search(collection_name=self.collection, query_vector=query_embedding, limit=top_k)
        return [{"id": r.id, "content": r.payload.get("content", ""), "score": r.score, "metadata": {k: v for k, v in r.payload.items() if k != "content"}} for r in results]

    async def clear(self):
        if not self.client:
            return
        self.client.delete_collection(self.collection)
        await self.initialize()

    async def get_stats(self) -> Dict[str, Any]:
        if not self.client or not self._healthy:
            return {"provider": "qdrant", "enabled": False}
        try:
            info = self.client.get_collection(self.collection)
            return {"provider": "qdrant", "enabled": True, "vectors": info.vectors_count, "points": info.points_count}
        except Exception as e:
            return {"provider": "qdrant", "enabled": False, "error": str(e)}


def get_vector_store() -> VectorStore:
    """Get the configured vector store."""
    if settings.vector_db_provider == "qdrant":
        return QdrantVectorStore()
    return VectorStore()
