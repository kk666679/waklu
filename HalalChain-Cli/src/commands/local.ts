import { Command } from 'commander'
import chalk from 'chalk'
import { createLocalModelsClient } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { heading, percent, printJson, printTable, scoreBar } from '../lib/format.js'
import { resolveText, splitList } from '../lib/input.js'
import { input } from '@inquirer/prompts'
import { isValidTemperature, toFloat, toInt } from '../lib/validators.js'

/**
 * Operator surface for `.halalchain/local-models` — the locally hosted models
 * that back the AI gateway when AI_BACKEND=local.
 */
export function createLocalCommand(): Command {
  const cmd = new Command('local')
    .alias('local-models')
    .description('Locally hosted model service (.halalchain/local-models)')

  cmd
    .command('status')
    .description('Show backend and loaded model count')
    .action(async () => {
      const client = createLocalModelsClient()
      const ready = await withSpinner('Checking local-models…', () =>
        client.get<Record<string, unknown>>('/health/ready')
      )
      heading('Local models')
      console.log(`  ${chalk.bold('Status:')}        ${String(ready['status'] ?? 'unknown')}`)
      console.log(`  ${chalk.bold('Service:')}       ${String(ready['service'] ?? 'local-models')}`)
      console.log(`  ${chalk.bold('Backend:')}       ${String(ready['backend'] ?? 'unknown')}`)
      console.log(`  ${chalk.bold('Models loaded:')} ${String(ready['models_loaded'] ?? 0)}`)
      console.log()
    })

  cmd
    .command('models')
    .description('List registered model names')
    .action(async () => {
      const client = createLocalModelsClient()
      const { models, backend } = await withSpinner('Listing models…', () => client.listModels())
      heading(`Models (${backend})`)
      if (models.length === 0) console.log(chalk.dim('  No models registered yet.'))
      else for (const m of models) console.log(`  • ${m}`)
      console.log()
    })

  cmd
    .command('generate')
    .description('Generate text with a locally hosted model')
    .option('-t, --text <text>', 'Prompt')
    .option('-f, --file <path>', 'Read the prompt from a file')
    .option('-m, --max-tokens <n>', 'Maximum tokens', '100')
    .option('-T, --temperature <n>', 'Sampling temperature (0-2)', '0.7')
    .option('--model <name>', 'Model name')
    .option('-j, --json', 'Output raw JSON')
    .action(
      async (opts: {
        text?: string
        file?: string
        maxTokens: string
        temperature: string
        model?: string
        json?: boolean
      }) => {
        const prompt = await resolveText(opts, 'Enter prompt:')
        const temperature = toFloat(opts.temperature, 0.7)
        if (!isValidTemperature(temperature)) throw new Error('--temperature must be between 0 and 2.')

        const client = createLocalModelsClient()
        const result = await withSpinner('Generating…', () =>
          client.generate(prompt, toInt(opts.maxTokens, 100), temperature, opts.model)
        )
        if (opts.json) {
          printJson(result)
          return
        }
        heading('Response')
        console.log(chalk.white(result.response))
        console.log(chalk.dim(`\n  Model: ${result.model} · temp: ${temperature}`))
        console.log()
      }
    )
  cmd
    .command('classify')
    .description('Classify text with a locally hosted model')
    .option('-t, --text <text>', 'Text to classify')
    .option('-f, --file <path>', 'Read text from a file')
    .option('-l, --labels <labels>', 'Comma-separated labels')
    .option('--model <name>', 'Model name')
    .option('-j, --json', 'Output raw JSON')
    .action(async (opts: { text?: string; file?: string; labels?: string; model?: string; json?: boolean }) => {
      const text = await resolveText(opts, 'Enter text:')
      let labels = splitList(opts.labels)
      if (labels.length === 0) {
        const answer = await input({ message: 'Labels (comma-separated):' })
        labels = splitList(typeof answer === 'string' ? answer : String(answer))
      }
      if (labels.length === 0) throw new Error('At least one label is required.')

      const client = createLocalModelsClient()
      const result = await withSpinner('Classifying…', () => client.classify(text, labels, opts.model))
      if (opts.json) {
        printJson(result)
        return
      }
      heading('Classification')
      console.log(`  ${chalk.bold('Best:')} ${chalk.green(result.bestLabel)} (${percent(result.bestScore)})`)
      console.log()
      for (const [label, score] of Object.entries(result.scores)) {
        console.log(`  ${label.padEnd(12)} ${chalk.cyan(scoreBar(score))} ${percent(score)}`)
      }
      console.log()
    })

  cmd
    .command('embed')
    .description('Embed text with a locally hosted model')
    .option('-t, --text <text>', 'Text to embed')
    .option('-f, --file <path>', 'Read text from a file')
    .option('--model <name>', 'Model name')
    .option('-j, --json', 'Output raw JSON')
    .action(async (opts: { text?: string; file?: string; model?: string; json?: boolean }) => {
      const text = await resolveText(opts, 'Enter text to embed:')
      const client = createLocalModelsClient()
      const result = await withSpinner('Embedding…', () => client.embeddings(text, opts.model))
      if (opts.json) {
        printJson(result)
        return
      }
      heading('Embedding')
      console.log(`  ${chalk.bold('Model:')}     ${result.model}`)
      console.log(`  ${chalk.bold('Dimension:')} ${result.embedding.length}`)
      console.log()
    })

  cmd
    .command('cache')
    .description('Show or clear the local-model cache')
    .option('--clear', 'Clear the cache instead of showing stats')
    .action(async (opts: { clear?: boolean }) => {
      const client = createLocalModelsClient()
      if (opts.clear) {
        await withSpinner('Clearing cache…', () => client.cacheClear())
        console.log(chalk.green('  ✔ Cache cleared'))
        console.log()
        return
      }
      const stats = await withSpinner('Fetching cache stats…', () => client.cacheStats())
      printTable(
        ['Key', 'Value'],
        Object.entries(stats).map(([k, v]) => [k, typeof v === 'object' ? JSON.stringify(v) : String(v)])
      )
      console.log()
    })

  return cmd
}

