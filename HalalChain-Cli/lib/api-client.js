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
      // AbortError on hung services — surface a useful message rather than
      // a raw TimeoutError.
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
}
