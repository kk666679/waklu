"""HalalChain memory package.

Public re-exports keep the spec's import path
``from .memory_manager import MemoryManager`` working.
"""
from .memory_manager import (
    MemoryEntry,
    MemoryManager,
    MemoryStatus,
    MemoryType,
    main,
)

__version__ = "3.0.0"

__all__ = [
    "MemoryEntry",
    "MemoryManager",
    "MemoryStatus",
    "MemoryType",
    "main",
    "short_term_memory",
    "long_term_memory",
    "episodic_memory",
    "semantic_memory",
    "procedural_memory",
    "memory_consolidation",
    "memory_retrieval",
]
