import { Command } from 'commander'
import chalk from 'chalk'
import { createAiInferenceClient } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { heading, printJson } from '../lib/format.js'
import { resolveText } from '../lib/input.js'
import { isValidTemperature, toFloat, toInt } from '../lib/validators.js'

/**
 * `llm generate` is the only llm route ai-inference exposes.
 *
 * The pre-merge CLI also shipped `llm chat`, `llm complete` and `llm models`,
 * which POSTed to /llm/chat, /llm/complete and /llm/models. None of those
 * routes exist — the service has a single /llm/generate. Those subcommands were
 * removed rather than ported: re-adding them would only restore 404s.
 */
export function createLlmCommand(): Command {
  const cmd = new Command('llm').description('LLM text generation (ai-inference)')

  cmd
    .command('generate')
    .alias('run')
    .description('Generate text from a prompt')
    .option('-t, --text <text>', 'Prompt')
    .option('-f, --file <path>', 'Read the prompt from a file')
    .option('-m, --max-tokens <n>', 'Maximum tokens to generate', '200')
    .option('-T, --temperature <n>', 'Sampling temperature (0-2)', '0.7')
    .option('-j, --json', 'Output raw JSON')
    .action(
      async (opts: {
        text?: string
        file?: string
        maxTokens: string
        temperature: string
        json?: boolean
      }) => {
        const prompt = await resolveText(opts, 'Enter prompt:')
        const temperature = toFloat(opts.temperature, 0.7)
        if (!isValidTemperature(temperature)) {
          throw new Error('--temperature must be between 0 and 2.')
        }
        const maxTokens = toInt(opts.maxTokens, 200)
        if (maxTokens < 1) throw new Error('--max-tokens must be at least 1.')

        const api = createAiInferenceClient()
        const result = await withSpinner('Generating…', () =>
          api.llmGenerate(prompt, maxTokens, temperature)
        )

        if (opts.json) {
          printJson(result)
          return
        }
        heading('Response')
        console.log(chalk.white(result.response))
        console.log(chalk.dim(`\n  Provider: ${result.provider} · max_tokens: ${maxTokens} · temp: ${temperature}`))
        console.log()
      }
    )

  return cmd
}
