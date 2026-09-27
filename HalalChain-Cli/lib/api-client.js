import chalk from "chalk";
import { Logger } from "./logger.js";

const DEFAULT_TIMEOUT_MS = 10_000;

export class APIClient {
  constructor(configManager) {
    this.config = configManager;
  }

  getBaseUrl() {
    return this.config.get("ai-inference.url", "http://localhost:7071");
  }

  getTawheedUrl() {
    return this.config.get("tawheed.url", "http://localhost:8000");
  }

  getLocalModelsUrl() {
    return this.config.get("local-models.url", "http://localhost:8080");
  }

  async request(method, path, body = null, { timeoutMs = DEFAULT_TIMEOUT_MS } = {}) {
    const url = this.getBaseUrl() + path;
    const headers = { "Content-Type": "application/json" };
    const apiKey = this.config.get("ai-inference.api-key");
    if (apiKey) headers["X-API-Key"] = apiKey;

    const opts = {
      method,
      headers,
      signal: AbortSignal.timeout(timeoutMs),
    };
    if (body && method !== "GET") opts.body = JSON.stringify(body);

    let resp;
    try {
      resp = await fetch(url, opts);
    } catch (err) {
      if (err.name === "TimeoutError" || err.name === "AbortError") {
        throw new Error(`Request to ${url} timed out after ${timeoutMs}ms`);
      }
      if (err.cause && err.cause.code === "ECONNREFUSED") {
        throw new Error(`Connection refused at ${url} — is the AI inference service running?`);
      }
      Logger.error("Network error:", err.message);
      throw err;
    }
    if (!resp.ok) {
      const text = await resp.text().catch(() => "");
      throw new Error(`HTTP ${resp.status} ${resp.statusText}: ${text.slice(0, 200)}`);
    }
    return resp.json();
  }

  // ai-inference endpoints
  async embeddings(text, model) {
    return this.request("POST", "/embeddings", { text, model });
  }

  async classify(text, labels, model) {
    return this.request("POST", "/classify", { text, labels, model });
  }

  async summarize(text, maxTokens, model) {
    return this.request("POST", "/summarize", { text, maxTokens, model });
  }

  async rerank(query, passages, model) {
    return this.request("POST", "/rerank", { query, passages, model });
  }

  async llmGenerate(prompt, maxTokens, temperature) {
    return this.request("POST", "/llm/generate", { prompt, max_tokens: maxTokens, temperature });
  }

  async llmChat(messages, model, temperature) {
    return this.request("POST", "/llm/chat", { messages, model, temperature });
  }

  async llmComplete(prompt, maxTokens) {
    return this.request("POST", "/llm/complete", { prompt, max_tokens: maxTokens });
  }

  async ingredientParse(text) {
    return this.request("POST", "/ingredient-parse", { text });
  }

  async certificateExtract(text) {
    return this.request("POST", "/certificate-extract", { text });
  }

  async ragAddDocuments(documents) {
    return this.request("POST", "/rag/add-documents", documents);
  }

  async ragSearch(query, topK) {
    return this.request("POST", "/rag/search", { query, top_k: topK });
  }

  async ragClear() {
    return this.request("POST", "/rag/clear");
  }

  async processDocument(filePath) {
    return this.request("POST", "/process/document", { file_path: filePath });
  }

  async health() {
    return this.request("GET", "/health/live");
  }

  async cacheStats() {
    return this.request("GET", "/cache/stats");
  }

  async cacheClear() {
    return this.request("POST", "/cache/clear");
  }
}

export class TawheedClient {
  constructor(configManager) {
    this.config = configManager;
  }

  getBaseUrl() {
    return this.config.get("tawheed.url", "http://localhost:8000");
  }

  async request(method, path, body = null, { timeoutMs = DEFAULT_TIMEOUT_MS } = {}) {
    const url = this.getBaseUrl() + path;
    const headers = { "Content-Type": "application/json" };
    const apiKey = this.config.get("tawheed.api-key");
    if (apiKey) headers["X-API-Key"] = apiKey;

    const opts = { method, headers, signal: AbortSignal.timeout(timeoutMs) };
    if (body && method !== "GET") opts.body = JSON.stringify(body);

    let resp;
    try {
      resp = await fetch(url, opts);
    } catch (err) {
      if (err.name === "TimeoutError" || err.name === "AbortError") {
        throw new Error(`Request to ${url} timed out after ${timeoutMs}ms`);
      }
      if (err.cause && err.cause.code === "ECONNREFUSED") {
        throw new Error(`Connection refused at ${url} — is Tawheed service running?`);
      }
      Logger.error("Network error:", err.message);
      throw err;
    }
    if (!resp.ok) {
      const text = await resp.text().catch(() => "");
      throw new Error(`HTTP ${resp.status} ${resp.statusText}: ${text.slice(0, 200)}`);
    }
    return resp.json();
  }

  async evaluatePolicy(productId, context) {
    return this.request("POST", "/api/v1/evaluate", { product_id: productId, context });
  }

  async queryEvidence(query, topK = 10) {
    return this.request("POST", "/api/v1/evidence/query", { query, top_k: topK });
  }

  async getEvidence(productId) {
    return this.request("GET", `/api/v1/evidence/${productId}`);
  }

  async health() {
    return this.request("GET", "/health");
  }
}

export class LocalModelsClient {
  constructor(configManager) {
    this.config = configManager;
  }

  getBaseUrl() {
    return this.config.get("local-models.url", "http://localhost:8080");
  }

  async request(method, path, body = null, { timeoutMs = DEFAULT_TIMEOUT_MS } = {}) {
    const url = this.getBaseUrl() + path;
    const headers = { "Content-Type": "application/json" };
    const apiKey = this.config.get("local-models.api-key");
    if (apiKey) headers["X-API-Key"] = apiKey;

    const opts = { method, headers, signal: AbortSignal.timeout(timeoutMs) };
    if (body && method !== "GET") opts.body = JSON.stringify(body);

    let resp;
    try {
      resp = await fetch(url, opts);
    } catch (err) {
      if (err.name === "TimeoutError" || err.name === "AbortError") {
        throw new Error(`Request to ${url} timed out after ${timeoutMs}ms`);
      }
      if (err.cause && err.cause.code === "ECONNREFUSED") {
        throw new Error(`Connection refused at ${url} — is local-models service running?`);
      }
      Logger.error("Network error:", err.message);
      throw err;
    }
    if (!resp.ok) {
      const text = await resp.text().catch(() => "");
      throw new Error(`HTTP ${resp.status} ${resp.statusText}: ${text.slice(0, 200)}`);
    }
    return resp.json();
  }

  async embeddings(text, model) {
    return this.request("POST", "/embeddings", { text, model });
  }

  async classify(text, labels, model) {
    return this.request("POST", "/classify", { text, labels, model });
  }

  async generate(prompt, maxTokens, temperature, model) {
    return this.request("POST", "/generate", { prompt, max_tokens: maxTokens, temperature, model });
  }

  async health() {
    return this.request("GET", "/health/ready");
  }

  async listModels() {
    return this.request("GET", "/models");
  }

  async cacheStats() {
    return this.request("GET", "/cache/stats");
  }

  async cacheClear() {
    return this.request("POST", "/cache/clear");
  }
}

// Factory functions for CLI config
export function createAIInferenceClient(config) {
  return new APIClient(config);
}

export function createTawheedClient(config) {
  return new TawheedClient(config);
}

export function createLocalModelsClient(config) {
  return new LocalModelsClient(config);
}