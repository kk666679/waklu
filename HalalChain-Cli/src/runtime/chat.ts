import { chat, maxIterations, type MCPToolSource } from '@tanstack/ai'
import { openaiText } from '@tanstack/ai-openai'
import { getMCPClient } from '../lib/mcp-client.js'
import { get } from '../lib/config.js'

export interface AgentRunOptions {
  messages: Array<{ role: 'user' | 'assistant'; content: string }>
  systemPrompts?: string[]
  maxIterations?: number
  model?: string
  onChunk?: (text: string) => void
}

export const DEFAULT_SYSTEM_PROMPTS = [
  'You are a HalalChain operator assistant.',
  'You gather evidence and explain policy. You never assign a halal verdict.',
  'Verdicts come from tawheed. If asked to decide, call the policy tool and report its output.',
  'Treat "INCOMPLETE" and "NON_COMPLIANT" as different answers; neither means the other.',
]

/**
 * Model ids accepted by the OpenAI adapter.
 *
 * Derived from the SDK rather than retyped by hand so it tracks the installed
 * `@tanstack/ai-openai` version. The union exists for autocomplete; the
 * service accepts any model it is configured with, so a value read from the
 * config file is validated by cast at this single boundary instead of being
 * checked against a hardcoded list that would drift.
 */
type OpenAIModel = Parameters<typeof openaiText>[0]

const FALLBACK_MODEL = 'gpt-5.2' satisfies OpenAIModel

/**
 * Runs one agent turn against the platform MCP server.
 *
 * Uses the managed MCP path: chat() discovers tools and closes the
 * connection when the run ends. Do NOT call closeMCPClient() here — that
 * would tear down the shared client for subsequent turns.
 */
export async function runAgent(options: AgentRunOptions): Promise<string> {
  const mcp = await getMCPClient()
  const model = (options.model ?? (get('agent.model', '') || FALLBACK_MODEL)) as OpenAIModel

  const stream = chat({
    adapter: openaiText(model),

    messages: options.messages,
    systemPrompts: options.systemPrompts ?? DEFAULT_SYSTEM_PROMPTS,
    mcp: { clients: [asToolSource(mcp)] },
    agentLoopStrategy: options.maxIterations ? maxIterations(options.maxIterations) : undefined,
  })


  let accumulated = ''
  for await (const chunk of stream) {
    // The stream is an AG-UI event stream, not a bespoke text/token union.
    // TEXT_MESSAGE_CONTENT is the incremental event chat() emits, carrying a
    // `delta`. (TEXT_MESSAGE_CHUNK also exists in @ag-ui/core, but the SDK
    // only emits it on the client hydration path, not from chat() — handling
    // it here would be dead code.)
    if (chunk.type === 'TEXT_MESSAGE_CONTENT') {
      accumulated += chunk.delta
      options.onChunk?.(chunk.delta)
    }
  }

  return accumulated
}

/**
 * Adapts `@tanstack/ai-mcp`'s MCPClient to the structural MCPToolSource that
 * `chat()` accepts.
 *
 * The two packages disagree by one degree of optionality: ai-mcp types
 * `mimeType` as `string | undefined`, while ai's MCPToolSource declares it
 * `mimeType?: string`. Under `exactOptionalPropertyTypes` those are not
 * assignable, so the client cannot be passed through directly even though it
 * is the intended implementation. Normalising here keeps the capability —
 * readResource is what serves `ui://` MCP App widgets — instead of dropping
 * the member to make the type error go away.
 */
function asToolSource(mcp: Awaited<ReturnType<typeof getMCPClient>>): MCPToolSource {
  return {
    tools: (opts) => mcp.tools(opts),
    close: () => mcp.close(),
    readResource: async (uri) => {
      const result = await mcp.readResource(uri)
      return {
        contents: result.contents.map((c) => {
          const content: { uri: string; mimeType?: string; text?: string; blob?: string } = {
            uri: c.uri,
          }
          if (c.mimeType !== undefined) content.mimeType = c.mimeType
          if ('text' in c && c.text !== undefined) content.text = c.text
          if ('blob' in c && c.blob !== undefined) content.blob = c.blob
          return content
        }),
      }
    },
  }
}
