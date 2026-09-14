from pydantic_settings import BaseSettings, SettingsConfigDict
from pydantic import Field
from typing import Optional


# Env-var aliases. The platform-wide canonical name is ``AI_GATEWAY_API_KEY``
# (set by docker-compose / .env.example), so we also accept it for the
# ``api_key`` field.
class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        case_sensitive=False,
        extra="ignore",
    )

    # App
    app_name: str = "halalchain-ai-inference"
    app_version: str = "0.2.0"
    port: int = 7071
    log_level: str = "INFO"
    environment: str = "development"
    demo_mode: bool = False

    # Auth — ai-inference refuses to start without an explicit API key in
    # non-dev environments (see src.auth.ensure_auth_configured).
    api_key: Optional[str] = Field(default=None, validation_alias="AI_GATEWAY_API_KEY")
    enable_auth: bool = True
    jwt_secret: Optional[str] = None
    jwt_algorithm: str = "HS256"
    jwt_expiration: int = 3600

    # CORS — explicit, comma-separated allow-list. Never "*" with credentials.
    allowed_origins: str = ""

    # Document processing — single configured root that the document
    # processor is allowed to read. Callers cannot escape this directory.
    document_root: str = ""
    upload_dir: str = ""

    # Cache
    enable_cache: bool = True
    cache_strategy: str = "lru"  # lru, redis
    cache_max_size: int = 10000
    cache_ttl: int = 3600

    # Redis
    redis_url: Optional[str] = None

    # Embeddings
    embedding_dim: int = 256
    embedding_provider: str = "local"  # local, sentence_transformers, openai, foundry, openclaw
    sentence_transformer_model: str = "all-MiniLM-L6-v2"

    # LLM
    default_llm_provider: str = "local"  # retained for backward-compat; use ai_backend
    openai_api_key: Optional[str] = None
    openai_model: str = "gpt-3.5-turbo"
    anthropic_api_key: Optional[str] = None
    anthropic_model: str = "claude-3-sonnet-20240229"
    anthropic_base_url: Optional[str] = None  # set to Ollama/OpenClaw Anthropic endpoint (no /v1)

    # AI Backend selector: local, openai, anthropic, foundry, openclaw
    # foundry  -> Azure AI Foundry (production), OpenAI-compatible endpoint
    # openclaw -> OpenClaw+Ollama gateway (dev/offline/CI), OpenAI-compatible
    ai_backend: str = "local"

    foundry_base_url: Optional[str] = None
    foundry_api_key: Optional[str] = None
    foundry_model: str = "halalchain-assistant"

    openclaw_base_url: Optional[str] = None
    openclaw_api_key: Optional[str] = None
    openclaw_model: str = "halalchain-assistant"

    # Vector DB
    vector_db_provider: str = "none"  # none, qdrant, chroma
    qdrant_host: str = "localhost"
    qdrant_port: int = 6333
    qdrant_collection: str = "halalchain_vectors"
    chroma_persist_dir: str = "./chroma_db"

    # RAG
    rag_chunk_size: int = 512
    rag_chunk_overlap: int = 50
    rag_top_k: int = 5
    rag_similarity_threshold: float = 0.7

    # Document Processing
    max_document_size: int = 10 * 1024 * 1024  # 10MB
    supported_file_types: list = [".pdf", ".docx", ".txt", ".md", ".html", ".xlsx"]


settings = Settings()
