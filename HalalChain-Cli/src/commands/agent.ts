import { Command } from 'commander'
import { runAgent } from '../runtime/chat.js'
import { logger } from '../lib/logger.js'

export const agentCommand = new Command('agent')
  .description('Interactive agent mode against the HalalChain MCP server')
  .argument('<prompt>', 'natural-language request')
  .option('-m, --max-iterations <n>', 'agent loop cap', '10')
  .action(async (prompt: string, opts: { maxIterations: string }) => {
    try {
      process.stdout.write('> ')
      await runAgent({
        messages: [{ role: 'user', content: prompt }],
        maxIterations: parseInt(opts.maxIterations, 10),
        onChunk: (text) => process.stdout.write(text),
      })
      process.stdout.write('\n')
    } catch (err) {
      logger.error(err instanceof Error ? err.message : String(err))
      process.exit(1)
    }
  })
