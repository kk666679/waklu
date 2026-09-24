"""Memory Manager — Aug 2026 skeleton."""
class MemoryManager:
    def __init__(self, config):
        self.config = config

    async def store(self, tier: str, key: str, value: dict):
        ...

    async def retrieve(self, tier: str, query: str, top_k: int = 5):
        ...
