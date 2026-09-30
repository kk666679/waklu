import { get as getConfig } from './config.js'

const DEFAULT_TIMEOUT_MS = 10_000

export interface RequestOptions {
  timeoutMs?: number
  signal?: AbortSignal
}

export type QueryValue = string | number | boolean

/**
 * Base HTTP client for the `.halalchain/*` FastAPI services.
 *
 * Every service speaks the same dialect: JSON in, JSON out, optional
 * `X-API-Key`, and health probes at `/health/live`. Centralising that here
 * means a command never hand-rolls a fetch and never has to remember that a
 * refused connection should read as "is tawheed running?" rather than
 * "ECONNREFUSED".
 */
export class ServiceClient {
  constructor(
    readonly serviceName: string,
    readonly configKey: string,
    private readonly apiKeyConfigKey: string | null = null
  ) {}

  get baseUrl(): string {
    const url = getConfig(this.configKey, '')
    if (!url) throw new Error(`No base URL configured for ${this.serviceName} (set ${this.configKey}).`)
    return url.replace(/\/+$/, '')
  }

  protected headers(): Record<string, string> {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' }
    if (this.apiKeyConfigKey) {
      const key = getConfig(this.apiKeyConfigKey, '')
      if (key) headers['X-API-Key'] = key
    }
    return headers
  }

  private buildUrl(path: string, query?: Record<string, QueryValue | undefined>): string {
    const url = new URL(this.baseUrl + path)
    for (const [k, v] of Object.entries(query ?? {})) {
      if (v !== undefined) url.searchParams.set(k, String(v))
    }
    return url.toString()
  }

  protected async request<T>(
    method: 'GET' | 'POST' | 'DELETE',
    path: string,
    body?: unknown,
    query?: Record<string, QueryValue | undefined>,
    opts: RequestOptions = {}
  ): Promise<T> {
    const timeoutMs = opts.timeoutMs ?? DEFAULT_TIMEOUT_MS
    const url = this.buildUrl(path, query)

    const init: RequestInit = {
      method,
      headers: this.headers(),
      signal: opts.signal ?? AbortSignal.timeout(timeoutMs),
    }
    // FastAPI distinguishes `body: list` (rag/add-documents) from scalar query
    // params, so only attach a body when one was actually supplied.
    if (body !== undefined && method !== 'GET') init.body = JSON.stringify(body)

    let resp: Response
    try {
      resp = await fetch(url, init)
    } catch (err) {
      throw this.describeNetworkError(err, url, timeoutMs)
    }

    if (!resp.ok) {
      const text = await resp.text().catch(() => '')
      throw new Error(`HTTP ${resp.status} ${resp.statusText} from ${this.serviceName}: ${text.slice(0, 200)}`)
    }

    if (resp.status === 204) return undefined as T
    return (await resp.json()) as T
  }

  private describeNetworkError(err: unknown, url: string, timeoutMs: number): Error {
    const e = err as { name?: string; message?: string; cause?: { code?: string } }
    if (e.name === 'TimeoutError' || e.name === 'AbortError') {
      return new Error(`Request to ${url} timed out after ${timeoutMs}ms`)
    }
    if (e.cause?.code === 'ECONNREFUSED') {
      return new Error(`Connection refused at ${url} — is the ${this.serviceName} service running?`)
    }
    return new Error(`Network error contacting ${this.serviceName}: ${e.message ?? String(err)}`)
  }

  get<T>(path: string, query?: Record<string, QueryValue | undefined>, opts?: RequestOptions): Promise<T> {
    return this.request<T>('GET', path, undefined, query, opts)
  }

  post<T>(
    path: string,
    body?: unknown,
    query?: Record<string, QueryValue | undefined>,
    opts?: RequestOptions
  ): Promise<T> {
    return this.request<T>('POST', path, body, query, opts)
  }

  /** Health probe with a short timeout, for `server status` / `cross health`. */
  async health(timeoutMs = 4_000): Promise<{ ok: boolean; status?: string; error?: string }> {
    try {
      const data = await this.get<{ status?: string }>('/health/live', undefined, { timeoutMs })
      return { ok: true, ...(data.status !== undefined ? { status: data.status } : {}) }
    } catch (err) {
      return { ok: false, error: (err as Error).message }
    }
  }
}
// ---------------------------------------------------------------------------
// Response shapes, taken from the Pydantic models in .halalchain/*/src.
// ---------------------------------------------------------------------------

export interface EmbeddingResponse {
  model: string
  embedding: number[]
  cached: boolean
}

export interface SummarizeResponse {
  model: string
  summary: string
}

export interface ClassifyResponse {
  model: string
  bestLabel: string
  bestScore: number
  scores: Record<string, number>
  cached: boolean
}

export interface RerankResponse {
  model: string
  results: { originalIndex: number; passage: string; score: number }[]
}

export interface IngredientItem {
  name: string
  percentage?: string | null
  eCode?: string | null
  risk: string
}

export interface IngredientParseResponse {
  model: string
  parsed: IngredientItem[]
  summary: Record<string, number>
}

export interface CertificateExtractResponse {
  model: string
  parsed: Record<string, string | null>
  completeness: string
}

export interface RagDocument {
  content: string
  id?: string
  source?: string
  metadata?: Record<string, unknown>
}

export interface RagSearchHit {
  content?: string
  score?: number
  metadata?: { source?: string; chunk_index?: number }
}

export interface RagSearchResponse {
  query: string
  results: RagSearchHit[]
}

export interface RagAddResponse {
  status: string
  chunks_added: number
}

/** `POST /v1/products/{id}/verify` -> `{"result": VerificationResult}` */
export interface VerificationResult {
  product_id: string
  status: string
  verification: Record<string, number>
  risk: { overall_risk: number }
  missing_evidence: string[]
  reason_codes: string[]
  policy_version: string
  jurisdiction: string
  requires_human_review: boolean
  verified_at: string
}

export interface VerifyProductResponse {
  result: VerificationResult
}

export interface HealthPayload {
  status?: string
  service?: string
  [k: string]: unknown
}

// ---------------------------------------------------------------------------
// ai-inference (:7071) — the AI gateway.
// ---------------------------------------------------------------------------

export class AiInferenceClient extends ServiceClient {
  constructor() {
    super('ai-inference', 'ai-inference.url', 'ai-inference.api-key')
  }

  embeddings(text: string, model?: string): Promise<EmbeddingResponse> {
    return this.post<EmbeddingResponse>('/embeddings', { text, model })
  }

  summarize(text: string, maxTokens?: number, model?: string): Promise<SummarizeResponse> {
    // `maxTokens` is camelCase in the Pydantic model — not max_tokens.
    return this.post<SummarizeResponse>('/summarize', { text, maxTokens, model })
  }

  classify(text: string, labels: string[], model?: string): Promise<ClassifyResponse> {
    return this.post<ClassifyResponse>('/classify', { text, labels, model })
  }

  rerank(query: string, passages: string[], model?: string): Promise<RerankResponse> {
    return this.post<RerankResponse>('/rerank', { query, passages, model })
  }

  ingredientParse(text: string): Promise<IngredientParseResponse> {
    return this.post<IngredientParseResponse>('/ingredient-parse', { text })
  }

  certificateExtract(text: string): Promise<CertificateExtractResponse> {
    return this.post<CertificateExtractResponse>('/certificate-extract', { text })
  }

  /** Body is a raw JSON *array*, not an object. */
  ragAddDocuments(documents: RagDocument[]): Promise<RagAddResponse> {
    return this.post<RagAddResponse>('/rag/add-documents', documents)
  }

  /** The service declares `query`/`top_k` as scalar params, so they ride the URL. */
  ragSearch(query: string, topK = 5): Promise<RagSearchResponse> {
    return this.post<RagSearchResponse>('/rag/search', undefined, { query, top_k: topK })
  }

  ragClear(): Promise<{ status: string; message: string }> {
    return this.post('/rag/clear')
  }

  /** Also query-param bound — a JSON body here is silently ignored by FastAPI. */
  llmGenerate(prompt: string, maxTokens = 100, temperature = 0.7): Promise<{
    prompt: string
    response: string
    provider: string
  }> {
    return this.post('/llm/generate', undefined, {
      prompt,
      max_tokens: maxTokens,
      temperature,
    })
  }

  /** Query-param bound too. */
  processDocument(filePath: string): Promise<unknown> {
    return this.post('/process/document', undefined, { file_path: filePath })
  }

  ready(): Promise<HealthPayload> {
    return this.get<HealthPayload>('/health/ready')
  }

  metrics(): Promise<Record<string, unknown>> {
    return this.get<Record<string, unknown>>('/metrics')
  }

  cacheStats(): Promise<Record<string, unknown>> {
    return this.get<Record<string, unknown>>('/cache/stats')
  }

  cacheClear(): Promise<{ status: string; message: string }> {
    return this.post('/cache/clear')
  }
}
// ---------------------------------------------------------------------------
// tawheed (:8000) — evidence collection + the deterministic Policy Engine.
// Its router is mounted at prefix `/v1` (see tawheed/src/api/routes.py).
// ---------------------------------------------------------------------------

export interface VerifyProductInput {
  jurisdiction?: string
  policy_version?: string
  document_text?: string
  certificate_number?: string
  certification_body?: string
  expiry_date?: string
  issue_date?: string
  scope?: string
  ingredients?: string
  supplier_name?: string
  supplier_country?: string
  supplier_status?: string
  vendor_country?: string
  documents?: string[]
}

export class TawheedClient extends ServiceClient {
  constructor() {
    super('tawheed', 'tawheed.url', 'tawheed.api-key')
  }

  /**
   * The only route that produces a compliance decision. The CLI forwards a
   * case and renders the verdict; it never computes one.
   */
  verifyProduct(productId: string, input: VerifyProductInput = {}): Promise<VerifyProductResponse> {
    return this.post<VerifyProductResponse>(`/v1/products/${encodeURIComponent(productId)}/verify`, input)
  }

  policies(): Promise<{ policies: string[] }> {
    return this.get<{ policies: string[] }>('/v1/policies')
  }

  agentHealth(): Promise<Record<string, unknown>> {
    return this.get<Record<string, unknown>>('/v1/agents/health')
  }

  /** Tawheed exposes a single `/health` (not `/health/live`). */
  override async health(timeoutMs = 4_000): Promise<{ ok: boolean; status?: string; error?: string }> {
    try {
      const data = await this.get<{ status?: string }>('/health', undefined, { timeoutMs })
      return { ok: true, ...(data.status !== undefined ? { status: data.status } : {}) }
    } catch (err) {
      return { ok: false, error: (err as Error).message }
    }
  }
}

// ---------------------------------------------------------------------------
// local-models (:8080) — locally hosted models behind the shared AI backend.
// ---------------------------------------------------------------------------

export interface GenerateResponse {
  model: string
  response: string
}

export class LocalModelsClient extends ServiceClient {
  constructor() {
    super('local-models', 'local-models.url', 'local-models.api-key')
  }

  embeddings(text: string, model?: string): Promise<EmbeddingResponse> {
    return this.post<EmbeddingResponse>('/embeddings', { text, model })
  }

  classify(text: string, labels: string[], model?: string): Promise<ClassifyResponse> {
    return this.post<ClassifyResponse>('/classify', { text, labels, model })
  }

  /** Unlike the gateway, this route takes a snake_case JSON body. */
  generate(prompt: string, maxTokens = 100, temperature = 0.7, model?: string): Promise<GenerateResponse> {
    return this.post<GenerateResponse>('/generate', {
      prompt,
      max_tokens: maxTokens,
      temperature,
      model,
    })
  }

  listModels(): Promise<{ models: string[]; backend: string }> {
    return this.get<{ models: string[]; backend: string }>('/models')
  }

  cacheStats(): Promise<Record<string, unknown>> {
    return this.get<Record<string, unknown>>('/cache/stats')
  }

  cacheClear(): Promise<{ status: string; message: string }> {
    return this.post('/cache/clear')
  }
}

// ---------------------------------------------------------------------------
// agents (:8081) — the evidence collection loop + eval DAG.
// ---------------------------------------------------------------------------

export interface AgentRunInput {
  [k: string]: unknown
}

export class AgentsClient extends ServiceClient {
  constructor() {
    super('agents', 'agents.url')
  }

  workflows(): Promise<{ workflows: { name: string; description?: string }[] }> {
    return this.get<{ workflows: { name: string; description?: string }[] }>('/api/v1/workflows')
  }

  /**
   * Run a collection workflow. The response envelope deliberately carries no
   * verdict-shaped field: this service collects evidence, the Policy Engine
   * decides. The CLI must not synthesise a status from this payload.
   */
  runWorkflow(workflow: string, input: AgentRunInput = {}): Promise<Record<string, unknown>> {
    return this.post<Record<string, unknown>>(
      `/api/v1/workflows/${encodeURIComponent(workflow)}/run`,
      { workflow, input }
    )
  }
}

// ---------------------------------------------------------------------------
// Factories
// ---------------------------------------------------------------------------

export function createAiInferenceClient(): AiInferenceClient {
  return new AiInferenceClient()
}

export function createTawheedClient(): TawheedClient {
  return new TawheedClient()
}

export function createLocalModelsClient(): LocalModelsClient {
  return new LocalModelsClient()
}

export function createAgentsClient(): AgentsClient {
  return new AgentsClient()
}


