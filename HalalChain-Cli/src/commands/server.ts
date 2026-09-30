import { Command } from 'commander'
import chalk from 'chalk'
import {
  createAgentsClient,
  createAiInferenceClient,
  createLocalModelsClient,
  createTawheedClient,
  type ServiceClient,
} from '../lib/api-client.js'
import { getAll } from '../lib/config.js'
import { withSpinner } from '../lib/async.js'
import { printTable } from '../lib/format.js'

function pythonSurfaces(): { name: string; client: ServiceClient; readyPath: string }[] {
  return [
    { name: 'ai-inference', client: createAiInferenceClient(), readyPath: '/health/ready' },
    { name: 'tawheed', client: createTawheedClient(), readyPath: '/v1/agents/health' },
    { name: 'local-models', client: createLocalModelsClient(), readyPath: '/health/ready' },
    { name: 'agents', client: createAgentsClient(), readyPath: '/health/live' },
  ]
}

export function createServerCommand(): Command {
  const cmd = new Command('server').description('Service health and local run instructions')

  cmd
    .command('status')
    .alias('health')
    .description('Check every configured service')
    .action(async () => {
      const rows: string[][] = []

      for (const { name, client } of pythonSurfaces()) {
        const result = await client.health()
        if (result.ok) {
          rows.push([chalk.green('up'), name, client.baseUrl, result.status ?? 'ok'])
        } else {
          rows.push([chalk.red('down'), name, client.baseUrl, result.error ?? 'unreachable'])
        }
      }

      // The .NET surfaces are probed with a plain fetch: the CLI does not
      // depend on the platform SDK, and a reachability check is all we need.
      const cfg = getAll()
      for (const [name, key] of [
        ['platform-api', 'platform-api.url'],
        ['marketplace', 'marketplace.url'],
        ['halalchain-web', 'halalchain.url'],
      ] as const) {
        const url = cfg[key] ?? ''
        try {
          const res = await fetch(url + '/health', { signal: AbortSignal.timeout(4000) })
          rows.push([
            res.ok ? chalk.green('up') : chalk.yellow('degraded'),
            name,
            url,
            `HTTP ${res.status}`,
          ])
        } catch (err) {
          rows.push([chalk.red('down'), name, url, (err as Error).message])
        }
      }

      printTable(['Status', 'Service', 'URL', 'Detail'], rows, [10, 16, 34, 44])
      console.log()
    })

  cmd
    .command('start')
    .description('Print the commands that start the local stack')
    .action(() => {
      console.log(chalk.bold('\n  Start the whole stack\n'))
      console.log(chalk.cyan('  docker compose up --build'))
      console.log(
        chalk.dim(
          '\n  Platform API 5001 · Marketplace 5201 · Customer UI 5200 · MCP 5002\n' +
            '  AI Gateway 7071 · Policy Engine 8000 · Local Models 8080 · Agents 8081\n'
        )
      )
      console.log(chalk.bold('\n  Or run one Python service directly\n'))
      for (const line of [
        'npm run ai:dev        # ai-inference on :7071',
        'npm run tawheed:dev   # tawheed on :8000',
      ]) {
        console.log(`  ${chalk.cyan(line)}`)
      }
      // local-models and agents have no dev script: the root package.json only
      // defines ai:dev and tawheed:dev. This used to advertise
      // `npm run local:dev` and `npm run agents:dev`, which exit with
      // "Missing script", so only real scripts are listed here.
      console.log(
        chalk.dim(
          '\n  local-models (:8080) and agents (:8081) have no dev script —\n' +
            '  start them with `docker compose up local-models agents`.'
        )
      )
      console.log()
    })

  cmd
    .command('metrics')
    .description('Show ai-inference metrics and cache stats')
    .action(async () => {
      const api = createAiInferenceClient()
      const { metrics, cache } = await withSpinner('Fetching ai-inference metrics…', async () => {
        const [m, c] = await Promise.all([api.metrics(), api.cacheStats()])
        return { metrics: m, cache: c }
      })
      printTable(['Metric', 'Value'], [
        ...Object.entries(metrics).map(([k, v]) => [k, typeof v === 'object' ? JSON.stringify(v) : String(v)]),
        ...Object.entries(cache).map(([k, v]) => [`cache.${k}`, typeof v === 'object' ? JSON.stringify(v) : String(v)]),
      ])
      console.log()
    })

  return cmd
}
