import { createMCPClient, type MCPClient } from '@tanstack/ai-mcp'

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
 */
export async function getMCPClient(options: MCPOptions = {}): Promise<MCPClient> {
  if (client) return client

  const url = options.url ?? process.env.HALALCHAIN_MCP_URL
  if (!url) {
    throw new Error(
      'HALALCHAIN_MCP_URL is not set. Point it at the platform MCP server, ' +
        'for example http://localhost:5002/mcp.'
    )
  }

  const token = options.token ?? process.env.HALALCHAIN_MCP_TOKEN

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
