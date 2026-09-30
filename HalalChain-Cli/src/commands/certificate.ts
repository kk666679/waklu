import { Command } from 'commander'
import chalk from 'chalk'
import { createAiInferenceClient } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { heading, printJson, printTable, scoreColor } from '../lib/format.js'
import { resolveText } from '../lib/input.js'

/** Field labels for the certificate extractor, in the order operators read them. */
const FIELD_LABELS: Record<string, string> = {
  certificate_number: 'Certificate number',
  certification_body: 'Certifying body',
  issue_date: 'Issue date',
  expiry_date: 'Expiry date',
  scope: 'Scope',
  products: 'Products covered',
}

export function createCertificateCommand(): Command {
  const cmd = new Command('certificate').alias('cert').description('Halal certificate field extraction')

  cmd
    .command('extract')
    .description('Extract structured fields from halal certificate text')
    .option('-t, --text <text>', 'Certificate text')
    .option('-f, --file <path>', 'Read certificate text from a file')
    .option('-j, --json', 'Output raw JSON')
    .action(async (opts: { text?: string; file?: string; json?: boolean }) => {
      const text = await resolveText(opts, 'Enter certificate text:')
      const api = createAiInferenceClient()
      const result = await withSpinner('Extracting certificate fields…', () =>
        api.certificateExtract(text)
      )

      if (opts.json) {
        printJson(result)
        return
      }

      heading('Certificate extraction')
      console.log(`  ${chalk.bold('Completeness:')} ${result.completeness}`)
      console.log(`  ${chalk.bold('Model:')}        ${result.model}`)
      console.log()

      // Print known fields in a stable order, then anything else the extractor
      // found that this CLI version does not yet know about.
      const known = Object.keys(FIELD_LABELS)
      const ordered = [
        ...known.filter((k) => k in result.parsed),
        ...Object.keys(result.parsed).filter((k) => !known.includes(k)),
      ]

      printTable(
        ['Field', 'Value'],
        ordered.map((k) => {
          const value = result.parsed[k]
          return [FIELD_LABELS[k] ?? k, value ? String(value) : chalk.dim('(not found)')]
        }),
        [24, 52]
      )
      console.log()
    })

  cmd
    .command('check')
    .description('Report how much of a certificate was recovered')
    .argument('<text>', 'certificate text')
    .action(async (text: string) => {
      const api = createAiInferenceClient()
      const result = await withSpinner('Extracting…', () => api.certificateExtract(text))
      const entries = Object.entries(result.parsed)
      const found = entries.filter(([, v]) => v !== null && v !== undefined && v !== '')
      const ratio = entries.length === 0 ? 0 : found.length / entries.length

      console.log(`  ${chalk.bold('Completeness:')} ${result.completeness}`)
      console.log(`  ${chalk.bold('Fields found:')} ${found.length}/${entries.length} (${scoreColor(ratio)(`${(ratio * 100).toFixed(0)}%`)})`)
      console.log()
      const missing = entries.filter(([, v]) => v === null || v === undefined || v === '')
      if (missing.length > 0) {
        console.log(chalk.yellow('  Still missing\n'))
        for (const [k] of missing) console.log(chalk.dim(`    • ${FIELD_LABELS[k] ?? k}`))
        console.log()
      }
      console.log(
        chalk.dim(
          '  Extraction is evidence collection. Run `halalchain evaluate` for a compliance status.\n'
        )
      )
    })

  return cmd
}
