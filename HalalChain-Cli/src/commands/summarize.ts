import { Command } from 'commander'
import chalk from 'chalk'
import { createAiInferenceClient } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { heading, printJson } from '../lib/format.js'
import { resolveText } from '../lib/input.js'
import { toInt } from '../lib/validators.js'

export function createSummarizeCommand(): Command {
  const cmd = new Command('summarize').alias('sum').description('Text summarization')

  cmd
    .command('generate')
    .alias('run')
    .description('Summarise a piece of text')
    .option('-t, --text <text>', 'Text to summarise')
    .option('-f, --file <path>', 'Read text from a file')
    .option('-m, --max-tokens <n>', 'Maximum summary tokens (1-500)', '120')
    .option('--model <name>', 'Model name')
    .option('-j, --json', 'Output raw JSON')
    .action(
      async (opts: { text?: string; file?: string; maxTokens: string; model?: string; json?: boolean }) => {
        const text = await resolveText(opts, 'Enter text to summarize:')
        const maxTokens = toInt(opts.maxTokens, 120)
        if (maxTokens < 1 || maxTokens > 500) {
          throw new Error('--max-tokens must be between 1 and 500.')
        }

        const api = createAiInferenceClient()
        const result = await withSpinner('Summarizing…', () =>
          api.summarize(text, maxTokens, opts.model)
        )

        if (opts.json) {
          printJson(result)
          return
        }
        heading('Summary')
        console.log(chalk.white(result.summary))
        console.log(
          chalk.dim(
            `\n  Model: ${result.model} · input ${text.split(/\s+/).filter(Boolean).length} words`
          )
        )
        console.log()
      }
    )

  return cmd
}
