from pydantic import BaseModel, Field
from typing import Optional, List, Dict, Any
from datetime import datetime


class EmbeddingRequest(BaseModel):
    text: str
    model: Optional[str] = None


class EmbeddingResponse(BaseModel):
    model: str
    embedding: List[float]
    cached: bool = False


class SummarizeRequest(BaseModel):
    text: str
    maxTokens: Optional[int] = Field(80, ge=1, le=500)
    model: Optional[str] = None


class SummarizeResponse(BaseModel):
    model: str
    summary: str


class ClassifyRequest(BaseModel):
    text: str
    labels: List[str]
    model: Optional[str] = None


class ClassifyResponse(BaseModel):
    model: str
    bestLabel: str
    bestScore: float
    scores: Dict[str, float]
    cached: bool = False


class RerankRequest(BaseModel):
    query: str
    passages: List[str]
    model: Optional[str] = None


class RankedPassage(BaseModel):
    originalIndex: int
    passage: str
    score: float


class RerankResponse(BaseModel):
    model: str
    results: List[RankedPassage]


class IngredientParseRequest(BaseModel):
    text: str


class IngredientItem(BaseModel):
    name: str
    percentage: Optional[str] = None
    eCode: Optional[str] = None
    risk: str


class IngredientParseResponse(BaseModel):
    model: str
    parsed: List[IngredientItem]
    summary: Dict[str, int]


class CertificateExtractRequest(BaseModel):
    text: str


class CertificateExtractResponse(BaseModel):
    model: str
    parsed: Dict[str, Optional[str]]
    completeness: str


class HealthResponse(BaseModel):
    service: str
    status: str
    utcNow: str
    uptime: int
    requestCount: int
    cache: Dict[str, Any]
