import { Command } from 'commander'
import chalk from 'chalk'
import { createTawheedClient, type VerifyProductInput } from '../lib/api-client.js'
import { get } from '../lib/config.js'
import { withSpinner } from '../lib/async.js'
import { printJson, printTable, percent, scoreColor } from '../lib/format.js'
import { readJsonFile } from '../lib/input.js'

/** Policy Engine outcomes (tawheed.core.models.ComplianceStatus). */
const STATUS_MEANING: Record<string, string> = {
  VERIFIED: 'Evidence satisfies the policy for this jurisdiction.',
  MANUAL_REVIEW: 'A human must review; the engine did not clear it.',
  INCOMPLETE: 'Required evidence is missing. This is not the same as non-compliant.',
  HOLD: 'Risk exceeded the hold threshold.',
  NON_COMPLIANT: 'Evidence contradicts the policy.',
  UNVERIFIED: 'Not enough evidence to decide either way.',
}

function statusColor(status: string): (s: string) => string {
  if (status === 'VERIFIED') return chalk.green
  if (status === 'NON_COMPLIANT' || status === 'HOLD') return chalk.red
  if (status === 'MANUAL_REVIEW') return chalk.yellow
  return chalk.cyan
}

interface EvaluateOpts {
  certificate_number?: string
  certification_body?: string
  ingredients?: string
  supplier_name?: string
  supplier_country?: string
  supplier_status?: string
  vendor_country?: string
  scope?: string
  issue_date?: string
  expiry_date?: string
  jurisdiction?: string
  policy_version?: string
  input?: string
  json?: boolean
}

export function createEvaluateCommand(): Command {
  const cmd = new Command('evaluate')
    .alias('eval')
    .description('Request a policy verdict from tawheed (the CLI never decides compliance itself)')
    .argument('<product-id>', 'product identifier to verify')
    .option('-c, --certificate-number <n>', 'certificate number')
    .option('-b, --certification-body <name>', 'certification body, e.g. JAKIM')
    .option('-i, --ingredients <text>', 'comma-separated ingredient list')
    .option('-s, --supplier-name <name>', 'supplier name')
    .option('--supplier-country <cc>', 'supplier country code')
    .option('--supplier-status <status>', 'supplier status')
    .option('--vendor-country <cc>', 'vendor country code')
    .option('--scope <text>', 'certification scope')
    .option('--issue-date <date>', 'certificate issue date')
    .option('--expiry-date <date>', 'certificate expiry date')
    .option('-j, --jurisdiction <code>', 'jurisdiction', get('tawheed.jurisdiction', 'MY'))
    .option('-p, --policy-version <v>', 'policy version', get('tawheed.policy-version', 'MY-v3'))
    .option('--input <path>', 'JSON file holding the evidence fields above')
    .option('--json', 'Output raw JSON')
    .action(async (productId: string, opts: EvaluateOpts) => {
      // --input supplies the whole evidence object; explicit flags override it.
      const base: VerifyProductInput = opts.input ? readJsonFile<VerifyProductInput>(opts.input) : {}

      const input: VerifyProductInput = {
        ...base,
        ...(opts.certificate_number !== undefined ? { certificate_number: opts.certificate_number } : {}),
        ...(opts.certification_body !== undefined ? { certification_body: opts.certification_body } : {}),
        ...(opts.ingredients !== undefined ? { ingredients: opts.ingredients } : {}),
        ...(opts.supplier_name !== undefined ? { supplier_name: opts.supplier_name } : {}),
        ...(opts.supplier_country !== undefined ? { supplier_country: opts.supplier_country } : {}),
        ...(opts.supplier_status !== undefined ? { supplier_status: opts.supplier_status } : {}),
        ...(opts.vendor_country !== undefined ? { vendor_country: opts.vendor_country } : {}),
        ...(opts.scope !== undefined ? { scope: opts.scope } : {}),
        ...(opts.issue_date !== undefined ? { issue_date: opts.issue_date } : {}),
        ...(opts.expiry_date !== undefined ? { expiry_date: opts.expiry_date } : {}),
      }
      // Commander always supplies a default for these two, so a flag always wins
      // over the value loaded from --input.
      input.jurisdiction = opts.jurisdiction ?? base.jurisdiction ?? 'MY'
      input.policy_version = opts.policy_version ?? base.policy_version ?? 'MY-v3'

      const tawheed = createTawheedClient()
      const { result } = await withSpinner(`Asking tawheed about ${productId}…`, () =>
        tawheed.verifyProduct(productId, input)
      )

      if (opts.json) {
        printJson(result)
        return
      }
      const paint = statusColor(result.status)
      console.log()
      console.log(`  ${chalk.bold('Product:')}      ${result.product_id}`)
      console.log(`  ${chalk.bold('Status:')}       ${paint(result.status)}`)
      console.log(`  ${chalk.dim(STATUS_MEANING[result.status] ?? '')}`)
      console.log(`  ${chalk.bold('Policy:')}       ${result.policy_version} (${result.jurisdiction})`)
      console.log(`  ${chalk.bold('Evaluated:')}    ${result.verified_at}`)
      console.log(
        `  ${chalk.bold('Human review:')} ${result.requires_human_review ? chalk.yellow('required') : 'not required'}`
      )
      console.log()

      console.log(chalk.bold('  Evidence signals\n'))
      printTable(
        ['Signal', 'Score'],
        Object.entries(result.verification).map(([k, v]) => [k, scoreColor(v)(percent(v, 0))])
      )
      console.log()
      console.log(
        `  ${chalk.bold('Overall risk:')} ${scoreColor(1 - result.risk.overall_risk)(percent(result.risk.overall_risk, 0))}`
      )
      console.log()

      if (result.missing_evidence.length > 0) {
        console.log(chalk.yellow('  Missing evidence\n'))
        for (const m of result.missing_evidence) console.log(chalk.dim(`    • ${m}`))
        console.log()
      }

      if (result.reason_codes.length > 0) {
        console.log(chalk.bold('  Reason codes\n'))
        for (const r of result.reason_codes) console.log(chalk.dim(`    • ${r}`))
        console.log()
      }
    })

  cmd
    .command('policies')
    .description('List policy versions known to tawheed')
    .action(async () => {
      const tawheed = createTawheedClient()
      const { policies } = await withSpinner('Fetching policy versions…', () => tawheed.policies())
      for (const p of policies) console.log(p)
      console.log()
    })

  cmd
    .command('agents-health')
    .description('Show tawheed evidence-agent health')
    .action(async () => {
      const tawheed = createTawheedClient()
      const data = await withSpinner('Fetching tawheed agent health…', () => tawheed.agentHealth())
      printJson(data)
    })

  cmd.addHelpText(
    'after',
    '\nExamples:\n' +
      '  halalchain evaluate PROD-42 --certificate-number MY-2024-001 -b JAKIM \\\n' +
      '    --ingredients "chicken, rice, palm oil" --expiry-date 2027-01-31\n' +
      '  halalchain evaluate PROD-42 --input case.json --json\n'
  )

  return cmd
}

