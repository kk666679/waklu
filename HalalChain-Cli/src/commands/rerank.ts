import { Command } from 'commander'
import chalk from 'chalk'
import { createAiInferenceClient } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { heading, percent, printJson, printTable, scoreColor, truncate } from '../lib/format.js'
import { readJsonFile, resolveText, splitList } from '../lib/input.js'
import { isValidTopK } from '../lib/validators.js'

export function createRerankCommand(): Command {
  const cmd = new Command('rerank').description('Rerank passages by relevance to a query')

  cmd
    .command('score')
    .description('Rerank passages for a query')
    .requiredOption('-q, --query <text>', 'Query text')
    .option('-p, --passages <passages...>', 'Passages to rerank')
    .option('-f, --file <path>', 'Read passages from a JSON array file')
    .option('-k, --top-k <n>', 'Keep only the top N results')
    .option('--model <name>', 'Model name')
    .option('-j, --json', 'Output raw JSON')
    .action(
      async (opts: {
        query: string
        passages?: string[]
        file?: string
        topK?: string
        model?: string
        json?: boolean
      }) => {
        let passages = opts.passages ?? []
        if (opts.file) {
          const parsed = readJsonFile<string[]>(opts.file)
          if (!Array.isArray(parsed)) throw new Error(`${opts.file} must contain a JSON array.`)
          passages = parsed
        }
        if (passages.length === 0) {
          throw new Error('Provide at least one passage via --passages or --file.')
        }
        if (opts.topK !== undefined && !isValidTopK(opts.topK)) {
          throw new Error('--top-k must be between 1 and 100.')
        }

        const api = createAiInferenceClient()
        const result = await withSpinner('Reranking…', () =>
          api.rerank(opts.query, passages, opts.model)
        )

        // The service returns every passage; --top-k trims for display only.
        const shown =
          opts.topK !== undefined ? result.results.slice(0, Number.parseInt(opts.topK, 10)) : result.results

        if (opts.json) {
          printJson(result)
          return
        }
        heading('Rerank results')
        console.log(`  ${chalk.bold('Query:')} ${truncate(opts.query, 90)}\n`)
        printTable(
          ['Rank', 'Score', 'Passage'],
          shown.map((r, i) => [i + 1, scoreColor(r.score)(percent(r.score)), truncate(r.passage, 60)]),
          [6, 9, 62]
        )
        console.log()
      }
    )

  cmd
    .command('labels')
    .description('Show how one passage ranks against several candidate queries')
    .requiredOption('-p, --passage <text>', 'The passage to probe')
    .requiredOption('-q, --queries <queries>', 'Comma-separated candidate queries')
    .option('-j, --json', 'Output raw JSON')
    .action(async (opts: { passage: string; queries: string; json?: boolean }) => {
      const queries = splitList(opts.queries)
      if (queries.length === 0) throw new Error('--queries must contain at least one query.')

      const api = createAiInferenceClient()
      const scored = await withSpinner(`Scoring against ${queries.length} queries…`, async () => {
        const out: { query: string; score: number }[] = []
        for (const q of queries) {
          const r = await api.rerank(q, [opts.passage])
          out.push({ query: q, score: r.results[0]?.score ?? 0 })
        }
        return out.sort((a, b) => b.score - a.score)
      })

      if (opts.json) {
        printJson(scored)
        return
      }
      heading('Passage ranking')
      printTable(
        ['Rank', 'Query', 'Score'],
        scored.map((s, i) => [i + 1, truncate(s.query, 50), scoreColor(s.score)(percent(s.score))]),
        [6, 52, 10]
      )
      console.log()
    })

  return cmd
}
