import { createMCPClient, type MCPClient } from '@tanstack/ai-mcp'
import { get } from './config.js'

let client: MCPClient | null = null

export interface MCPOptions {
  url?: string
  token?: string
}

/**
 * Process-level MCP client singleton.
 *
 * Never close between agent turns. chat() executes MCP tools lazily while
 * the response streams; closing in a finally around chat() kills in-flight
 * tool calls. If you need per-run lifecycle, pass the client to chat() via
 * the `mcp` prop and let chat() own close().
 *
 * URL and token resolve through the config store (and therefore honour
 * HALALCHAIN_MCP_URL / HALALCHAIN_MCP_TOKEN), so `halalchain config set mcp.url`
 * and the environment are interchangeable.
 */
export async function getMCPClient(options: MCPOptions = {}): Promise<MCPClient> {
  if (client) return client

  const url = options.url ?? get('mcp.url', '')
  if (!url) {
    throw new Error(
      'No MCP endpoint configured. Run `halalchain config set mcp.url <url>` ' +
        '(the platform MCP server listens on http://localhost:5002/mcp) or export ' +
        'HALALCHAIN_MCP_URL.'
    )
  }

  const token = options.token ?? get('mcp.token', '')


  // `headers` is omitted rather than set to undefined: under
  // exactOptionalPropertyTypes, `{ headers: undefined }` is not assignable to
  // `headers?: Record<string, string>`. Spreading keeps the unauthenticated
  // case type-correct instead of forcing a cast.
  client = await createMCPClient({
    transport: {
      type: 'http',
      url,
      ...(token ? { headers: { Authorization: `Bearer ${token}` } } : {}),
    },
  })

  return client
}

export async function closeMCPClient(): Promise<void> {
  if (!client) return
  await client.close()
  client = null
}

/** Register a process-exit handler once. Call from src/index.ts. */
export function registerMCPCleanup(): void {
  const cleanup = async () => {
    await closeMCPClient().catch(() => undefined)
  }
  process.once('SIGINT', cleanup)
  process.once('SIGTERM', cleanup)
  process.once('beforeExit', cleanup)
}
