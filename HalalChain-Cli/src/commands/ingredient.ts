import { Command } from 'commander'
import chalk from 'chalk'
import { createAiInferenceClient, type IngredientItem } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { heading, printJson, printTable } from '../lib/format.js'
import { resolveText } from '../lib/input.js'

/** Risk values the ingredient parser emits. */
const RISK_COLOR: Record<string, (s: string) => string> = {
  halal: chalk.green,
  permissible: chalk.green,
  questionable: chalk.yellow,
  mashbooh: chalk.yellow,
  haram: chalk.red,
  prohibited: chalk.red,
}

function riskColor(risk: string): (s: string) => string {
  return RISK_COLOR[risk.toLowerCase()] ?? chalk.dim
}

function render(items: IngredientItem[], summary: Record<string, number>, model: string): void {
  heading('Ingredient analysis')
  if (items.length === 0) {
    console.log(chalk.dim('  No ingredients parsed.'))
    console.log()
    return
  }
  printTable(
    ['#', 'Ingredient', 'Risk', 'E-num', 'Share'],
    items.map((item, i) => [
      i + 1,
      item.name,
      riskColor(item.risk)(item.risk),
      item.eCode ?? '—',
      item.percentage ?? '—',
    ]),
    [4, 30, 12, 12, 11]
  )
  console.log()
  const counts = Object.entries(summary)
  if (counts.length > 0) {
    console.log(
      chalk.dim(
        `  ${counts.map(([k, v]) => `${riskColor(k)(k)}: ${v}`).join('  ·  ')}   (model ${model})`
      )
    )
  }
  console.log()
}

export function createIngredientCommand(): Command {
  const cmd = new Command('ingredient').alias('ing').description('Ingredient parsing and risk analysis')

  cmd
    .command('parse')
    .description('Parse an ingredient list and flag risky components')
    .option('-t, --text <text>', 'Ingredient list')
    .option('-f, --file <path>', 'Read the ingredient list from a file')
    .option('-j, --json', 'Output raw JSON')
    .action(async (opts: { text?: string; file?: string; json?: boolean }) => {
      const text = await resolveText(opts, 'Enter ingredient list:')
      const api = createAiInferenceClient()
      const result = await withSpinner('Parsing ingredients…', () => api.ingredientParse(text))
      if (opts.json) printJson(result)
      else render(result.parsed, result.summary, result.model)
    })

  cmd
    .command('flagged')
    .description('List only the non-halal ingredients, for a quick triage view')
    .argument('<text>', 'ingredient list')
    .action(async (text: string) => {
      const api = createAiInferenceClient()
      const result = await withSpinner('Parsing ingredients…', () => api.ingredientParse(text))
      const flagged = result.parsed.filter(
        (i) => !['halal', 'permissible'].includes(i.risk.toLowerCase())
      )
      heading('Flagged ingredients')
      if (flagged.length === 0) {
        console.log(chalk.green('  Nothing flagged. Every parsed item came back halal/permissible.'))
      } else {
        for (const item of flagged) {
          console.log(`  ${riskColor(item.risk)('•')} ${chalk.bold(item.name)} ${chalk.dim(item.risk)}`)
        }
      }
      console.log()
      console.log(chalk.dim('  A parser flag is evidence, not a verdict. Use `halalchain evaluate`.\n'))
    })

  return cmd
}
