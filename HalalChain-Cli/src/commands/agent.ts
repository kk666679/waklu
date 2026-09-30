import { Command } from 'commander'
import { runAgent } from '../runtime/chat.js'
import { get } from '../lib/config.js'
import { toInt } from '../lib/validators.js'

export function createAgentCommand(): Command {
  return new Command('agent')
    .description('Natural-language agent mode against the HalalChain MCP server')
    .argument('<prompt>', 'what you want to know or do')
    .option('-m, --max-iterations <n>', 'agent loop cap', get('agent.max-iterations', '10'))
    .option('--model <name>', 'model to use', get('agent.model', 'gpt-5.2'))
    .option('--json', 'Buffer the answer and print it as a JSON string')
    .action(async (prompt: string, opts: { maxIterations: string; model: string; json?: boolean }) => {
      const maxIterations = toInt(opts.maxIterations, 10)
      if (maxIterations < 1) throw new Error('--max-iterations must be at least 1.')

      let answer = ''
      // In --json mode nothing is written until the turn completes, so the
      // output stays a single parseable value.
      const onChunk = opts.json ? (text: string) => { answer += text } : (text: string) => { process.stdout.write(text) }

      await runAgent({
        messages: [{ role: 'user', content: prompt }],
        maxIterations,
        model: opts.model,
        onChunk,
      })

      if (opts.json) {
        process.stdout.write(`${JSON.stringify(answer)}\n`)
      } else {
        process.stdout.write('\n')
      }
    })
}

export const agentCommand = createAgentCommand()

