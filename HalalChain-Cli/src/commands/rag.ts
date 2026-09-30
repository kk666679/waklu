import { Command } from 'commander'
import chalk from 'chalk'
import { confirm } from '@inquirer/prompts'
import { createAiInferenceClient, type RagDocument, type RagSearchResponse } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { heading, percent, printJson, printTable, scoreColor, truncate } from '../lib/format.js'
import { readJsonFile, resolveText } from '../lib/input.js'
import { toInt } from '../lib/validators.js'
import { isValidTopK } from '../lib/validators.js'

/**
 * Render a vector-search result set.
 *
 * Shared by `rag search` and `vector search` so the two never drift in how
 * they present the same payload.
 */
export function renderRagSearch(query: string, result: RagSearchResponse, json: boolean): void {
  if (json) {
    printJson(result)
    return
  }
  heading('Vector search')
  console.log(`  ${chalk.bold('Query:')} ${truncate(query, 90)}\n`)
  if (result.results.length === 0) {
    console.log(chalk.dim('  No matches. The store may be empty — try `halalchain rag add`.'))
    console.log()
    return
  }
  printTable(
    ['#', 'Score', 'Source', 'Content'],
    result.results.map((r, i) => [
      i + 1,
      scoreColor(r.score)(percent(r.score)),
      r.metadata?.source ?? 'unknown',
      truncate(r.content ?? '', 54),
    ]),
    [4, 9, 22, 56]
  )
  console.log()
}

export function createRagCommand(): Command {
  const cmd = new Command('rag').description('RAG document management (ai-inference vector store)')

  cmd
    .command('add')
    .description('Add documents to the vector store')
    .option('-f, --file <path>', 'JSON file with an array of {content, source} objects')
    .option('-c, --content <text>', 'A single document body')
    .option('-s, --source <url>', 'Source label for --content')
    .option('--id <id>', 'Document id for --content')
    .action(async (opts: { file?: string; content?: string; source?: string; id?: string }) => {
      if (opts.file && opts.content) throw new Error('Use either --file or --content, not both.')

      let documents: RagDocument[]
      if (opts.file) {
        const parsed = readJsonFile<RagDocument[]>(opts.file)
        if (!Array.isArray(parsed)) throw new Error(`${opts.file} must contain a JSON array of documents.`)
        documents = parsed
      } else {
        const content = await resolveText({ text: opts.content }, 'Document content:')
        documents = [
          {
            content,
            ...(opts.id !== undefined ? { id: opts.id } : {}),
            source: opts.source ?? 'cli',
          },
        ]
      }

      const api = createAiInferenceClient()
      const result = await withSpinner(`Adding ${documents.length} document(s)…`, () =>
        api.ragAddDocuments(documents)
      )
      console.log(chalk.green(`  ✔ ${result.chunks_added} chunk(s) added`))
      console.log()
    })

  cmd
    .command('search')
    .alias('query')
    .description('Search the vector store')
    .argument('<query>', 'search query')
    .option('-k, --top-k <n>', 'Number of results (1-100)', '5')
    .option('-j, --json', 'Output raw JSON')
    .action(async (query: string, opts: { topK: string; json?: boolean }) => {
      if (!isValidTopK(opts.topK)) throw new Error('--top-k must be between 1 and 100.')
      const api = createAiInferenceClient()
      const result = await withSpinner('Searching…', () => api.ragSearch(query, toInt(opts.topK, 5)))
      renderRagSearch(query, result, opts.json === true)
    })

  cmd
    .command('clear')
    .description('Remove every document from the vector store')
    .option('-f, --force', 'Skip the confirmation prompt')
    .action(async (opts: { force?: boolean }) => {
      if (!opts.force) {
        const ok = await confirm({
          message: 'Clear ALL documents from the vector store?',
          default: false,
        })
        if (!ok) {
          console.log('Aborted.')
          return
        }
      }
      const api = createAiInferenceClient()
      await withSpinner('Clearing vector store…', () => api.ragClear())
      console.log(chalk.green('  ✔ Vector store cleared'))
      console.log()
    })

  cmd
    .command('cache')
    .description('Show ai-inference cache statistics')
    .action(async () => {
      const api = createAiInferenceClient()
      const stats = await withSpinner('Fetching cache stats…', () => api.cacheStats())
      printTable(
        ['Key', 'Value'],
        Object.entries(stats).map(([k, v]) => [k, typeof v === 'object' ? JSON.stringify(v) : String(v)])
      )
      console.log()
    })

  return cmd
}
