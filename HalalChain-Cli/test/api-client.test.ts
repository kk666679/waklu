import { test } from 'node:test'
import assert from 'node:assert/strict'
import { createServer, type IncomingMessage, type Server, type ServerResponse } from 'node:http'
import type { AddressInfo } from 'node:net'
import { mkdtempSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

/**
 * Contract tests for the `.halalchain` HTTP clients.
 *
 * These pin the wire format against the real FastAPI routes, because the
 * pre-merge CLI got several of them wrong in ways a unit test with a mocked
 * fetch would never catch:
 *
 *   - tawheed's router is mounted at `/v1`, and the verdict route is
 *     `POST /v1/products/{id}/verify`. The old client called `/api/v1/evaluate`
 *     and `/api/v1/evidence/*`, none of which exist.
 *   - `/rag/search`, `/llm/generate` and `/process/document` declare scalar
 *     params, so they travel on the query string. Sending a JSON body is
 *     silently ignored by FastAPI — the request succeeds and returns garbage.
 *   - `/rag/add-documents` takes a bare JSON array, not an object.
 *
 * A stub server records the method, path, query and body so each assertion
 * describes the actual request that goes over the wire.
 */
interface Recorded {
  method: string
  path: string
  query: Record<string, string>
  headers: IncomingMessage['headers']
  body: string
}

async function withStubServer(
  handler: (req: IncomingMessage, res: ServerResponse, body: string) => void,
  run: (baseUrl: string, recorded: Recorded[]) => Promise<void>
): Promise<void> {
  const recorded: Recorded[] = []
  const server: Server = createServer((req, res) => {
    const chunks: Buffer[] = []
    req.on('data', (c: Buffer) => chunks.push(c))
    req.on('end', () => {
      const body = Buffer.concat(chunks).toString('utf8')
      const url = new URL(req.url ?? '/', 'http://localhost')
      recorded.push({
        method: req.method ?? '',
        path: url.pathname,
        query: Object.fromEntries(url.searchParams),
        headers: req.headers,
        body,
      })
      handler(req, res, body)
    })
  })

  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve))
  const { port } = server.address() as AddressInfo

  // The client resolves its base URL through the config store, so point every
  // service at the stub for the duration of this test.
  const home = mkdtempSync(join(tmpdir(), 'halalchain-client-'))
  process.env['HOME'] = home
  process.env['USERPROFILE'] = home
  process.env['AI_INFERENCE_URL'] = `http://127.0.0.1:${port}`
  process.env['TAWHEED_URL'] = `http://127.0.0.1:${port}`
  process.env['LOCAL_MODELS_URL'] = `http://127.0.0.1:${port}`
  process.env['AGENTS_URL'] = `http://127.0.0.1:${port}`

  try {
    const mod = await import(`../src/lib/api-client.js?case=${Math.random()}`)
    await run(`http://127.0.0.1:${port}`, recorded, mod)
  } finally {
    for (const k of ['AI_INFERENCE_URL', 'TAWHEED_URL', 'LOCAL_MODELS_URL', 'AGENTS_URL']) {
      delete process.env[k]
    }
    await new Promise<void>((resolve) => server.close(() => resolve()))
  }
}

function json(res: ServerResponse, payload: unknown, status = 200): void {
  res.writeHead(status, { 'content-type': 'application/json' })
  res.end(JSON.stringify(payload))
}
test('tawheed: verifyProduct posts to /v1/products/{id}/verify', async () => {
  await withStubServer(
    (_req, res) => {
      json(res, {
        result: {
          product_id: 'PROD-42',
          status: 'VERIFIED',
          verification: { certificate_verification: 0.9 },
          risk: { overall_risk: 0.1 },
          missing_evidence: [],
          reason_codes: [],
          policy_version: 'MY-v3',
          jurisdiction: 'MY',
          requires_human_review: false,
          verified_at: '2026-01-01T00:00:00Z',
        },
      })
    },
    async (_base, recorded, mod) => {
      const tawheed = new mod.TawheedClient()
      const { result } = await tawheed.verifyProduct('PROD-42', {
        certificate_number: 'MY-2024-001',
        certification_body: 'JAKIM',
      })

      const call = recorded[0]
      assert.ok(call, 'a request must be recorded')
      assert.equal(call.method, 'POST')
      assert.equal(call.path, '/v1/products/PROD-42/verify')
      assert.equal(call.headers['content-type'], 'application/json')
      assert.deepEqual(JSON.parse(call.body), {
        certificate_number: 'MY-2024-001',
        certification_body: 'JAKIM',
      })
      assert.equal(result.status, 'VERIFIED')
    }
  )
})

test('tawheed: product ids are URL-encoded', async () => {
  await withStubServer(
    (_req, res) => json(res, { result: { status: 'UNVERIFIED' } }),
    async (_base, recorded, mod) => {
      await new mod.TawheedClient().verifyProduct('vendor/42 space')
      assert.equal(recorded[0]?.path, '/v1/products/vendor%2F42%20space/verify')
    }
  )
})

test('tawheed: policies and agentHealth use the /v1 prefix', async () => {
  await withStubServer(
    (_req, res) => json(res, { policies: ['MY-v3'] }),
    async (_base, recorded, mod) => {
      const tawheed = new mod.TawheedClient()
      await tawheed.policies()
      assert.equal(recorded[0]?.path, '/v1/policies')

      await tawheed.agentHealth()
      assert.equal(recorded[1]?.path, '/v1/agents/health')
    }
  )
})

test('tawheed: health probes GET /health, not /health/live', async () => {
  await withStubServer(
    (_req, res) => json(res, { status: 'ok', service: 'tawheed' }),
    async (_base, recorded, mod) => {
      const result = await new mod.TawheedClient().health()
      assert.equal(result.ok, true)
      assert.equal(recorded[0]?.path, '/health')
    }
  )
})

test('ai-inference: ragSearch sends query and top_k as query params', async () => {
  await withStubServer(
    (_req, res) => json(res, { query: 'halal', results: [] }),
    async (_base, recorded, mod) => {
      await new mod.AiInferenceClient().ragSearch('halal certificate', 7)

      const call = recorded[0]
      assert.ok(call)
      assert.equal(call.method, 'POST')
      assert.equal(call.path, '/rag/search')
      assert.equal(call.query['query'], 'halal certificate')
      assert.equal(call.query['top_k'], '7')
      // A JSON body here would be silently discarded by FastAPI.
      assert.equal(call.body, '')
    }
  )
})

test('ai-inference: llmGenerate sends prompt/max_tokens/temperature as query params', async () => {
  await withStubServer(
    (_req, res) => json(res, { prompt: 'hi', response: 'hello', provider: 'demo' }),
    async (_base, recorded, mod) => {
      await new mod.AiInferenceClient().llmGenerate('hi', 50, 0.2)

      const call = recorded[0]
      assert.ok(call)
      assert.equal(call.path, '/llm/generate')
      assert.equal(call.query['prompt'], 'hi')
      assert.equal(call.query['max_tokens'], '50')
      assert.equal(call.query['temperature'], '0.2')
      assert.equal(call.body, '')
    }
  )
})
test('ai-inference: processDocument sends file_path as a query param', async () => {
  await withStubServer(
    (_req, res) => json(res, { status: 'success' }),
    async (_base, recorded, mod) => {
      await new mod.AiInferenceClient().processDocument('/docs/cert.pdf')
      assert.equal(recorded[0]?.path, '/process/document')
      assert.equal(recorded[0]?.query['file_path'], '/docs/cert.pdf')
    }
  )
})

test('ai-inference: ragAddDocuments posts a bare JSON array', async () => {
  await withStubServer(
    (_req, res) => json(res, { status: 'success', chunks_added: 2 }),
    async (_base, recorded, mod) => {
      await new mod.AiInferenceClient().ragAddDocuments([
        { content: 'halal', source: 'cli' },
        { content: 'haram', source: 'cli' },
      ])

      const call = recorded[0]
      assert.ok(call)
      assert.equal(call.path, '/rag/add-documents')
      const parsed: unknown = JSON.parse(call.body)
      assert.ok(Array.isArray(parsed), 'body must be a JSON array, not an object')
      assert.equal((parsed as unknown[]).length, 2)
    }
  )
})

test('ai-inference: summarize uses the camelCase maxTokens field', async () => {
  await withStubServer(
    (_req, res) => json(res, { model: 'm', summary: 's' }),
    async (_base, recorded, mod) => {
      await new mod.AiInferenceClient().summarize('long text', 42)
      const call = recorded[0]
      assert.ok(call)
      assert.equal(call.path, '/summarize')
      // The Pydantic model is `maxTokens`; `max_tokens` would 422.
      assert.equal(JSON.parse(call.body)['maxTokens'], 42)
    }
  )
})

test('ai-inference: the API key travels as X-API-Key when configured', async () => {
  const home = mkdtempSync(join(tmpdir(), 'halalchain-apikey-'))
  process.env['HOME'] = home
  process.env['USERPROFILE'] = home

  const seen: (string | undefined)[] = []
  const server = createServer((req, res) => {
    seen.push(req.headers['x-api-key'] as string | undefined)
    json(res, { model: 'm', embedding: [0.1], cached: false })
  })
  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve))
  const { port } = server.address() as AddressInfo
  process.env['AI_INFERENCE_URL'] = `http://127.0.0.1:${port}`

  try {
    const mod = await import(`../src/lib/api-client.js?case=${Math.random()}`)
    // Deliberately NOT cache-busted. A `?case=` suffix here would create a
    // second, independent config module instance, so the `set()` below would
    // write to one CONFIG_PATH while the client read the store through the
    // instance api-client.ts actually imported — and the header never appeared.
    const config = await import('../src/lib/config.js')
    config.set('ai-inference.api-key', 'secret-key-value')
    await new mod.AiInferenceClient().embeddings('hi')
    assert.equal(seen[0], 'secret-key-value')
  } finally {
    delete process.env['AI_INFERENCE_URL']
    await new Promise<void>((resolve) => server.close(() => resolve()))
  }
})
test('agents: runWorkflow posts {workflow, input} to the run route', async () => {
  await withStubServer(
    (_req, res) => json(res, { workflow: 'supplier_onboarding', evidence: [] }),
    async (_base, recorded, mod) => {
      await new mod.AgentsClient().runWorkflow('supplier_onboarding', { vendor_id: 'V-1' })

      const call = recorded[0]
      assert.ok(call)
      assert.equal(call.path, '/api/v1/workflows/supplier_onboarding/run')
      assert.deepEqual(JSON.parse(call.body), {
        workflow: 'supplier_onboarding',
        input: { vendor_id: 'V-1' },
      })
    }
  )
})

test('local-models: generate takes a snake_case JSON body', async () => {
  await withStubServer(
    (_req, res) => json(res, { model: 'default', response: 'ok' }),
    async (_base, recorded, mod) => {
      await new mod.LocalModelsClient().generate('hello', 10, 0.5, 'default')
      const call = recorded[0]
      assert.ok(call)
      assert.equal(call.path, '/generate')
      // Unlike the gateway's /llm/generate, this route reads a JSON body.
      assert.deepEqual(JSON.parse(call.body), {
        prompt: 'hello',
        max_tokens: 10,
        temperature: 0.5,
        model: 'default',
      })
    }
  )
})

test('clients: a non-2xx response raises with the status and a body excerpt', async () => {
  await withStubServer(
    (_req, res) => json(res, { detail: 'not found' }, 404),
    async (_base, _recorded, mod) => {
      await assert.rejects(
        () => new mod.AiInferenceClient().embeddings('hi'),
        /HTTP 404 .*from ai-inference/
      )
    }
  )
})

test('clients: a refused connection names the service that is down', async () => {
  // Bind a port and immediately release it, so we get a real closed port.
  // Hard-coding 1 does not work: undici rejects it before connecting with
  // cause.message "bad port" and no `code`, so describeNetworkError fell
  // through to its generic branch and never produced "Connection refused".
  const probe = createServer()
  await new Promise<void>((resolve) => probe.listen(0, '127.0.0.1', resolve))
  const deadPort = (probe.address() as AddressInfo).port
  await new Promise<void>((resolve) => probe.close(() => resolve()))

  process.env['TAWHEED_URL'] = `http://127.0.0.1:${deadPort}`

  try {
    const mod = await import(`../src/lib/api-client.js?case=${Math.random()}`)
    await assert.rejects(() => new mod.TawheedClient().policies(), /Connection refused .*tawheed/)
  } finally {
    delete process.env['TAWHEED_URL']
  }
})

test('clients: health() reports failure without throwing', async () => {
  const home = mkdtempSync(join(tmpdir(), 'halalchain-health-'))
  process.env['HOME'] = home
  process.env['USERPROFILE'] = home
  process.env['AGENTS_URL'] = 'http://127.0.0.1:1'

  try {
    const mod = await import(`../src/lib/api-client.js?case=${Math.random()}`)
    const result = await new mod.AgentsClient().health(500)
    assert.equal(result.ok, false)
    assert.ok(result.error && result.error.length > 0)
  } finally {
    delete process.env['AGENTS_URL']
  }
})



