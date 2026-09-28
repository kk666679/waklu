import { chat } from '@tanstack/ai'
import { openaiText } from '@tanstack/ai-openai'
import { getMCPClient } from '../lib/mcp-client.js'

export interface AgentRunOptions {
  messages: Array<{ role: 'user' | 'assistant'; content: string }>
  systemPrompts?: string[]
  maxIterations?: number
  onChunk?: (text: string) => void
}

const DEFAULT_SYSTEM_PROMPTS = [
  'You are a HalalChain operator assistant.',
  'You gather evidence and explain policy. You never assign a halal verdict.',
  'Verdicts come from tawheed. If asked to decide, call the policy tool and report its output.',
]

/**
 * Runs one agent turn against the platform MCP server.
 *
 * Uses the managed MCP path: chat() discovers tools and closes the
 * connection when the run ends. Do NOT call closeMCPClient() here — that
 * would tear down the shared client for subsequent turns.
 */
export async function runAgent(options: AgentRunOptions): Promise<string> {
  const mcp = await getMCPClient()

  const stream = chat({
    adapter: openaiText('gpt-5.2'),
    messages: options.messages,
    systemPrompts: options.systemPrompts ?? DEFAULT_SYSTEM_PROMPTS,
    mcp: { clients: [mcp] },
    agentLoopStrategy: options.maxIterations
      ? { maxIterations: options.maxIterations }
      : undefined,
  })

  let accumulated = ''
  for await (const chunk of stream) {
    if (chunk.type === 'text') {
      accumulated += chunk.content
      options.onChunk?.(chunk.content)
    }
  }

  return accumulated
}
