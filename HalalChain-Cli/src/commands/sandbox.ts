import { Command } from 'commander'
import chalk from 'chalk'
import { promises as fs } from 'node:fs'
import { tmpdir } from 'node:os'
import path from 'node:path'
import {
  NETWORK_ISOLATION,
  classifyDenial,
  runInSandbox,
  sandboxSupported,
} from '../lib/sandbox.js'
import { heading, printJson } from '../lib/format.js'
import { isValidUrl, toInt } from '../lib/validators.js'

const MAX_TIMEOUT_MS = 300_000
const SANDBOX_PREFIX = 'halalchain-sandbox-'

/**
 * `halalchain sandbox` — run a script under Node's permission model.
 *
 * This isolates the filesystem, child processes, workers, addons, WASI and the
 * inspector. It does NOT isolate the network: the permission model has no
 * egress control, so `doctor` and `--help` say so rather than implying a
 * guarantee that does not exist.
 */
export function createSandboxCommand(): Command {
  const cmd = new Command('sandbox')
    .alias('sbx')
    .description(
      'Run a script under the Node permission model (filesystem, child processes and workers denied)'
    )

  cmd
    .command('doctor')
    .description('Report whether this runtime can enforce the sandbox')
    .option('-j, --json', 'Output raw JSON')
    .action(async (opts: { json?: boolean }) => {
      const support = await sandboxSupported()
      const info = {
        node: process.version,
        supported: support.ok,
        reason: support.reason,
        filesystem: 'isolated',
        childProcess: 'denied',
        worker: 'denied',
        addons: 'denied',
        wasi: 'denied',
        inspector: 'denied',
        network: NETWORK_ISOLATION,
      }
      if (opts.json) {
        printJson(info)
        return
      }
      heading('Sandbox support')
      console.log(`  ${chalk.bold('Node:')}        ${info.node}`)
      console.log(
        `  ${chalk.bold('Available:')}    ${
          support.ok ? chalk.green('yes') : chalk.red('no')
        }${support.reason ? chalk.dim(` — ${support.reason}`) : ''}`
      )
      console.log(`  ${chalk.bold('Filesystem:')}   ${info.filesystem}`)
      console.log(`  ${chalk.bold('Child proc:')}  ${info.childProcess}`)
      console.log(`  ${chalk.bold('Worker:')}      ${info.worker}`)
      console.log(
        `  ${chalk.bold('Network:')}     ${
          info.network === 'none' ? chalk.yellow('not isolated') : info.network
        }`
      )
      console.log()
      if (info.network === 'none') {
        console.log(
          chalk.dim(
            '  The permission model has no egress control. A sandboxed script can\n' +
              '  still open sockets. Treat it as a filesystem/process jail, not an\n' +
              '  air gap.\n'
          )
        )
      }
    })

  cmd
    .command('run')
    .description('Run a script in the sandbox')
    .argument('<script>', 'path to a .js/.mjs script, or inline source with --inline')
    .option('--inline', 'treat the argument as inline source rather than a path')
    .option('-t, --timeout <ms>', 'hard timeout in milliseconds', '60000')
    .option('--env <pairs...>', 'KEY=VALUE variables to pass (parent env is not inherited)')
    .option('--read <paths...>', 'extra filesystem roots the script may read')
    .option('--write <paths...>', 'extra filesystem roots the script may write')
    .option('--tenant <id>', 'inject HALALCHAIN_TENANT_ID')
    .option('--keep', 'keep the sandbox directory for inspection')
    .option('-j, --json', 'Output raw JSON')
    .action(
      async (
        script: string,
        opts: {
          inline?: boolean
          timeout: string
          env?: string[]
          read?: string[]
          write?: string[]
          tenant?: string
          keep?: boolean
          json?: boolean
        }
      ) => {
        const timeoutMs = toInt(opts.timeout, 60_000)
        if (timeoutMs < 1 || timeoutMs > MAX_TIMEOUT_MS) {
          throw new Error(`--timeout must be between 1 and ${MAX_TIMEOUT_MS} ms.`)
        }

        const env: Record<string, string> = {}
        for (const pair of opts.env ?? []) {
          const eq = pair.indexOf('=')
          if (eq <= 0) throw new Error(`--env expects KEY=VALUE, got "${pair}".`)
          env[pair.slice(0, eq)] = pair.slice(eq + 1)
        }
        // A script must never be handed a credential by accident: the parent
        // environment is not inherited, and the sandbox has no network
        // isolation, so a secret placed here would not be contained.
        for (const key of Object.keys(env)) {
          if (/_KEY$|_TOKEN$|_SECRET$|_PASSWORD$/.test(key)) {
            throw new Error(
              `Refusing to pass ${key} into the sandbox: the sandbox has no ` +
                'network isolation, so a secret placed here would not be contained.'
            )
          }
        }

        for (const p of [...(opts.read ?? []), ...(opts.write ?? [])]) {
          if (!path.isAbsolute(p)) throw new Error(`Path must be absolute: ${p}`)
        }

        const result = await runInSandbox({
          script,
          inline: Boolean(opts.inline),
          timeoutMs,
          env,
          allowFsRead: opts.read ?? [],
          allowFsWrite: opts.write ?? [],
          ...(opts.tenant ? { tenantId: opts.tenant } : {}),
          keepDir: Boolean(opts.keep),
        })

        const denial = classifyDenial(result.stderr)
        if (opts.json) {
          printJson({ ...result, denial: denial ?? null })
        } else {
          if (result.stdout) process.stdout.write(result.stdout)
          if (result.stderr) process.stderr.write(result.stderr)
          heading('Sandbox result')
          console.log(`  ${chalk.bold('Exit:')}     ${result.exitCode ?? 'null'}`)
          console.log(`  ${chalk.bold('Duration:')} ${result.durationMs} ms`)
          if (result.timedOut) console.log(`  ${chalk.yellow('Timed out and was killed.')}`)
          if (result.outputTruncated) console.log(`  ${chalk.yellow('Output truncated.')}`)
          if (denial) console.log(`  ${chalk.yellow(`Denied: ${denial}`)}`)
          if (opts.keep) console.log(`  ${chalk.dim(`Sandbox dir: ${result.sandboxDir}`)}`)
          console.log()
        }

        if (denial || result.timedOut || (result.exitCode ?? 1) !== 0) {
          process.exitCode = result.exitCode ?? 1
        }
      }
    )

  cmd
    .command('clean')
    .description('Remove leftover sandbox directories from the temp folder')
    .action(async () => {
      const entries = await fs.readdir(tmpdir()).catch((): string[] => [])
      const dirs = entries.filter((e) => e.startsWith(SANDBOX_PREFIX))
      if (dirs.length === 0) {
        console.log(chalk.dim('  No sandbox directories found.'))
        console.log()
        return
      }
      for (const d of dirs) {
        await fs.rm(path.join(tmpdir(), d), { recursive: true, force: true }).catch(
          () => undefined
        )
        console.log(`  ${chalk.green('✔')} removed ${d}`)
      }
      console.log()
    })

  cmd.addHelpText(
    'after',
    '\nExamples:\n' +
      '  halalchain sandbox doctor\n' +
      '  halalchain sandbox run --inline "console.log(process.version)"\n' +
      '  halalchain sandbox run ./job.mjs --timeout 10000\n' +
      '  halalchain sandbox run ./job.mjs --env MODE=demo --read ./fixtures\n'
  )

  return cmd
}

