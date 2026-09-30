import { Command } from 'commander'
import chalk from 'chalk'
import { writeFileSync } from 'node:fs'
import { createAiInferenceClient } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { cosineSimilarity, heading, percent, printJson, scoreColor, truncate } from '../lib/format.js'
import { readJsonFile, resolveText } from '../lib/input.js'

export function createEmbeddingCommand(): Command {
  const cmd = new Command('embedding').alias('embed').description('Embedding generation and comparison')

  cmd
    .command('generate')
    .description('Generate an embedding for a piece of text')
    .option('-t, --text <text>', 'Text to embed')
    .option('-f, --file <path>', 'Read text from a file')
    .option('-m, --model <name>', 'Model name')
    .option('-j, --json', 'Output raw JSON')
    .action(async (opts: { text?: string; file?: string; model?: string; json?: boolean }) => {
      const text = await resolveText(opts, 'Enter text to embed:')
      const api = createAiInferenceClient()
      const result = await withSpinner('Generating embedding…', () =>
        api.embeddings(text, opts.model)
      )

      if (opts.json) {
        printJson(result)
        return
      }
      heading('Embedding')
      console.log(`  ${chalk.bold('Model:')}      ${result.model}`)
      console.log(`  ${chalk.bold('Dimension:')}  ${result.embedding.length}`)
      console.log(
        `  ${chalk.bold('Preview:')}    ${result.embedding.slice(0, 8).map((v) => v.toFixed(4)).join(', ')} ...`
      )
      console.log(`  ${chalk.bold('Cached:')}     ${result.cached}`)
      console.log()
    })

  cmd
    .command('compare')
    .description('Compare two texts by cosine similarity of their embeddings')
    .argument('<text-a>', 'first text')
    .argument('<text-b>', 'second text')
    .option('-j, --json', 'Output raw JSON')
    .action(async (textA: string, textB: string, opts: { json?: boolean }) => {
      const api = createAiInferenceClient()
      const [a, b] = await withSpinner('Comparing embeddings…', () =>
        Promise.all([api.embeddings(textA), api.embeddings(textB)])
      )
      const similarity = cosineSimilarity(a.embedding, b.embedding)

      if (opts.json) {
        printJson({ similarity, textA, textB, model: a.model })
        return
      }
      heading('Similarity')
      console.log(`  ${chalk.bold('Text A:')}      ${truncate(textA)}`)
      console.log(`  ${chalk.bold('Text B:')}      ${truncate(textB)}`)
      console.log(`  ${chalk.bold('Similarity:')} ${scoreColor(similarity)(percent(similarity))}`)
      console.log()
    })

  cmd
    .command('batch')
    .description('Embed many texts from a JSON array')
    .requiredOption('-f, --file <path>', 'JSON file containing an array of strings')
    .option('-o, --output <path>', 'Write results to this JSON file')
    .action(async (opts: { file: string; output?: string }) => {
      const texts = readJsonFile<string[]>(opts.file)
      if (!Array.isArray(texts)) {
        throw new Error(`${opts.file} must contain a JSON array of strings.`)
      }
      const api = createAiInferenceClient()
      const results = await withSpinner(`Embedding ${texts.length} text(s)…`, async () => {
        const out: { text: string; embedding: number[] }[] = []
        // Sequential on purpose: the gateway caches per text and a burst of
        // parallel requests just queue behind its own worker pool.
        for (const text of texts) {
          const r = await api.embeddings(text)
          out.push({ text, embedding: r.embedding })
        }
        return out
      })

      if (opts.output) {
        writeFileSync(opts.output, JSON.stringify(results, null, 2) + '\n', 'utf8')
        console.log(chalk.green(`  Saved ${results.length} embeddings → ${opts.output}`))
      } else {
        printJson(results)
      }
    })

  return cmd
}
