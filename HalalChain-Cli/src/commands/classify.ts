import { Command } from 'commander'
import chalk from 'chalk'
import { createAiInferenceClient, type ClassifyResponse } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { heading, percent, printJson, scoreBar, scoreColor } from '../lib/format.js'
import { resolveText, splitList } from '../lib/input.js'
import { input } from '@inquirer/prompts'

const HALAL_LABELS = ['halal', 'haram', 'mashbooh', 'unknown']

function render(result: ClassifyResponse): void {
  heading('Classification')
  console.log(`  ${chalk.bold('Best label:')} ${chalk.green(result.bestLabel)}`)
  console.log(`  ${chalk.bold('Confidence:')} ${percent(result.bestScore)}`)
  console.log(`  ${chalk.bold('Model:')}      ${result.model}`)
  console.log()
  for (const [label, score] of Object.entries(result.scores)) {
    const line = `  ${label.padEnd(12)} ${chalk.cyan(scoreBar(score))} ${percent(score)}`
    console.log(score === result.bestScore ? chalk.green(line) : line)
  }
  console.log()
}

export function createClassifyCommand(): Command {
  const cmd = new Command('classify').description('Text classification')

  cmd
    .command('halal')
    .description('Classify text as halal / haram / mashbooh / unknown')
    .option('-t, --text <text>', 'Text to classify')
    .option('-f, --file <path>', 'Read text from a file')
    .option('-m, --model <name>', 'Model name')
    .option('-j, --json', 'Output raw JSON')
    .action(async (opts: { text?: string; file?: string; model?: string; json?: boolean }) => {
      const text = await resolveText(opts, 'Enter text to classify:')
      const api = createAiInferenceClient()
      const result = await withSpinner('Classifying…', () =>
        api.classify(text, HALAL_LABELS, opts.model)
      )
      if (opts.json) printJson(result)
      else render(result)
    })

  cmd
    .command('generic')
    .description('Classify text against your own label set')
    .option('-t, --text <text>', 'Text to classify')
    .option('-f, --file <path>', 'Read text from a file')
    .option('-l, --labels <labels>', 'Comma-separated labels')
    .option('-m, --model <name>', 'Model name')
    .option('-j, --json', 'Output raw JSON')
    .action(
      async (opts: { text?: string; file?: string; labels?: string; model?: string; json?: boolean }) => {
        const text = await resolveText(opts, 'Enter text to classify:')
        let labels = splitList(opts.labels)
        if (labels.length === 0) {
          const answer = await input({ message: 'Labels (comma-separated):' })
          labels = splitList(typeof answer === 'string' ? answer : String(answer))
        }
        if (labels.length === 0) throw new Error('At least one label is required.')

        const api = createAiInferenceClient()
        const result = await withSpinner('Classifying…', () => api.classify(text, labels, opts.model))
        if (opts.json) printJson(result)
        else render(result)
      }
    )

  cmd
    .command('risk')
    .description('Score the best label against a confidence floor')
    .argument('<text>', 'text to classify')
    .option('--min <pct>', 'confidence floor, 0-100', '70')
    .action(async (text: string, opts: { min: string }) => {
      const floor = Number.parseFloat(opts.min) / 100
      const api = createAiInferenceClient()
      const result = await withSpinner('Classifying…', () => api.classify(text, HALAL_LABELS))
      const above = result.bestScore >= floor
      console.log(
        `  ${chalk.bold('Label:')}      ${result.bestLabel} (${percent(result.bestScore)})`
      )
      console.log(
        `  ${chalk.bold('Floor:')}      ${percent(floor, 0)} → ${
          above ? chalk.green('above floor') : chalk.yellow('below floor — needs review')
        }`
      )
      console.log(
        chalk.dim(
          '\n  A low confidence here is not a verdict. It means the classifier did not find a signal.'
        )
      )
      console.log()
      if (!above) process.exitCode = 2
    })

  return cmd
}
