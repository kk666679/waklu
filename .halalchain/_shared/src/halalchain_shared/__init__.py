"""Shared runtime helpers for the HalalChain Python services.

The two Python services — `.halalchain/ai-inference` and
`.halalchain/tawheed` — are independently deployable. They do NOT share
business logic, but they share:

* the canonical ``AI_BACKEND`` selector and the env-var name normalization
  for all LLM providers (OpenAI, Anthropic, Azure AI Foundry, OpenClaw/Ollama);
* an in-process LRU cache that can be promoted to Redis via a strategy flag.

This package is intentionally tiny. It only contains things that are
genuinely identical across both services. Provider-specific business logic
stays in the service that uses it.
"""

__version__ = "0.1.0"
