import { Command } from 'commander'
import chalk from 'chalk'
import { createAgentsClient } from '../lib/api-client.js'
import { withSpinner } from '../lib/async.js'
import { printJson, printTable } from '../lib/format.js'
import { parseJsonOption, readJsonFile } from '../lib/input.js'

/**
 * Operator surface for `.halalchain/agents` — the evidence collection loop and
 * the eval DAG that `HalalChain.Agents` scores.
 *
 * This service has no verdict authority: it returns evidence proposals, and
 * `halalchain evaluate` is the only path to a compliance status. The command
 * prints the payload as-is and never derives a status from it.
 */
export function createAgentsCommand(): Command {
  const cmd = new Command('agents')
    .alias('agent-service')
    .description('Evidence collection workflows (.halalchain/agents)')

  cmd
    .command('workflows')
    .alias('list')
    .description('List workflows this deployment can run')
    .action(async () => {
      const client = createAgentsClient()
      const { workflows } = await withSpinner('Fetching workflows…', () => client.workflows())
      if (workflows.length === 0) {
        console.log(chalk.dim('  No workflows registered.'))
        return
      }
      printTable(
        ['Workflow', 'Description'],
        workflows.map((w) => [w.name, w.description ?? ''])
      )
      console.log()
    })

  cmd
    .command('run <workflow>')
    .description('Run a workflow and print the evidence it collected')
    .option('-i, --input <json>', 'workflow input as inline JSON')
    .option('-f, --file <path>', 'workflow input as a JSON file')
    .option('-j, --json', 'Output raw JSON')
    .action(async (workflow: string, opts: { input?: string; file?: string; json?: boolean }) => {
      if (opts.input && opts.file) {
        throw new Error('Use either --input or --file, not both.')
      }
      const input = opts.file
        ? readJsonFile<Record<string, unknown>>(opts.file)
        : parseJsonOption<Record<string, unknown>>(opts.input, '--input')

      const client = createAgentsClient()
      const result = await withSpinner(`Running ${workflow}…`, () => client.runWorkflow(workflow, input))

      if (opts.json) {
        printJson(result)
        return
      }

      // Render the envelope generically: the schema is owned by the service and
      // deliberately has no verdict field for us to interpret.
      printJson(result)
      console.log(
        chalk.dim(
          '\n  This is evidence, not a verdict. Run `halalchain evaluate <product-id>` for compliance.'
        )
      )
    })

  cmd
    .command('health')
    .description('Check the agents service')
    .action(async () => {
      const client = createAgentsClient()
      const result = await client.health()
      if (result.ok) {
        console.log(chalk.green(`  ✔ agents up (${client.baseUrl}) — ${result.status ?? 'ok'}`))
      } else {
        console.log(chalk.red(`  ✖ agents unreachable (${client.baseUrl}) — ${result.error}`))
        process.exitCode = 1
      }
      console.log()
    })

  return cmd
}
