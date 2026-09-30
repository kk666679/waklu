import { Command } from 'commander'
import chalk from 'chalk'
import { createAiInferenceClient } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { heading, printJson, printTable, truncate } from '../lib/format.js'
import { toInt } from '../lib/validators.js'
import { isValidTopK } from '../lib/validators.js'
import { renderRagSearch } from './rag.js'

/**
 * `vector` is the semantic-search view over the same ai-inference vector store
 * that `rag` manages. The `halalchain-vector` skill documents
 * `halalchain vector search`, so that subcommand exists here; `rag` remains
 * the place for writes.
 */
export function createVectorCommand(): Command {
  const cmd = new Command('vector').description('Vector store search and statistics')

  cmd
    .command('search')
    .alias('query')
    .description('Semantic search over indexed evidence')
    .argument('<query>', 'natural-language query')
    .option('-k, --top-k <n>', 'Number of results (1-100)', '5')
    .option('-j, --json', 'Output raw JSON')
    .action(async (query: string, opts: { topK: string; json?: boolean }) => {
      if (!isValidTopK(opts.topK)) throw new Error('--top-k must be between 1 and 100.')
      const api = createAiInferenceClient()
      const result = await withSpinner('Searching vector store…', () =>
        api.ragSearch(query, toInt(opts.topK, 5))
      )
      renderRagSearch(query, result, opts.json === true)
    })

  cmd
    .command('stats')
    .description('Show vector store readiness and dependencies')
    .option('-j, --json', 'Output raw JSON')
    .action(async (opts: { json?: boolean }) => {
      const api = createAiInferenceClient()
      const ready = await withSpinner('Fetching vector store status…', () => api.ready())

      if (opts.json) {
        printJson(ready)
        return
      }

      heading('Vector store')
      console.log(`  ${chalk.bold('Status:')}        ${ready.status ?? 'unknown'}`)
      console.log(`  ${chalk.bold('Service:')}       ${ready.service ?? 'ai-inference'}`)
      const cache = ready.cache as Record<string, unknown> | undefined
      if (cache) {
        console.log(
          `  ${chalk.bold('Cache:')}         size=${String(cache['size'] ?? 0)} hits=${String(cache['hits'] ?? 0)} misses=${String(cache['misses'] ?? 0)}`
        )
      }
      if (Array.isArray(ready.dependencies)) {
        console.log()
        printTable(
          ['Dependency', 'Detail'],
          (ready.dependencies as unknown[]).map((d) => [truncate(JSON.stringify(d), 40), ''])
        )
      }
      console.log()
    })

  return cmd
}
