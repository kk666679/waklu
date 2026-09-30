import { Command } from 'commander'
import { input, confirm } from '@inquirer/prompts'
import chalk from 'chalk'
import {
  CONFIG_PATH,
  DEFAULTS,
  KNOWN_KEYS,
  get,
  getAll,
  getAllRedacted,
  isKnownKey,
  mask,
  reset,
  set,
  unset,
} from '../lib/config.js'
import { JURISDICTIONS, generateSecret, isJurisdiction, validateJwtKey } from '../lib/secrets.js'
import { isValidUrl } from '../lib/validators.js'
import { choose } from '../lib/input.js'

const URL_KEYS = [
  'platform-api.url',
  'marketplace.url',
  'halalchain.url',
  'ai-inference.url',
  'tawheed.url',
  'local-models.url',
  'agents.url',
] as const

const JURISDICTION_NAMES: Record<string, string> = {
  MY: 'Malaysia (JAKIM / MS1500:2019)',
  ID: 'Indonesia (MUI / BPJPH)',
  SG: 'Singapore',
  BN: 'Brunei',
  GCC: 'Gulf',
  EU: 'Europe',
}

export function createConfigCommand(): Command {
  const cmd = new Command('config').description(`Manage local HalalChain config (${CONFIG_PATH})`)

  cmd
    .command('init')
    .description('Interactive setup wizard')
    .action(async () => {
      console.log(chalk.bold('\nHalalChain config init\n'))
      const current = getAll()

      for (const key of URL_KEYS) {
        const answer = await input({ message: key, default: current[key] ?? DEFAULTS[key] })
        set(key, typeof answer === 'string' ? answer.trim() : String(answer))
      }

      const jurisdiction = await choose(
        'Default jurisdiction',
        JURISDICTIONS.map((j) => ({ value: j, name: `${j} — ${JURISDICTION_NAMES[j] ?? j}` })),
        current['tawheed.jurisdiction'] ?? 'MY'
      )
      set('tawheed.jurisdiction', jurisdiction)
      // MY ships a versioned policy set; every other jurisdiction starts at v1.
      set('tawheed.policy-version', jurisdiction === 'MY' ? 'MY-v3' : `${jurisdiction}-v1`)

      const generate = await confirm({
        message: 'Generate a new JWT secret key?',
        default: !current['jwt.key'],
      })
      if (generate) {
        const key = generateSecret(32)
        set('jwt.key', key)
        console.log(chalk.yellow('\n  JWT key (shown once — put it in your .env):\n'))
        console.log(chalk.cyan(`  ${key}\n`))
      } else if (current['jwt.key']) {
        console.log(chalk.dim('  Keeping the existing JWT key.'))
      }

      console.log(chalk.green(`✔ Config saved → ${CONFIG_PATH}`))
    })

  cmd
    .command('get <key>')
    .description('Print one config value')
    .action((key: string) => {
      if (!isKnownKey(key)) {
        console.error(chalk.red(`Unknown key: ${key}`))
        console.error(chalk.dim('Run `halalchain config list` to see valid keys.'))
        process.exit(1)
      }
      const value = get(key)
      if (value === undefined) {
        console.error(chalk.red(`Unknown key: ${key}`))
        process.exit(1)
      }
      console.log(value)
    })

  cmd
    .command('set <key> <value>')
    .description('Persist a config value to ~/.halalchain/config.json')
    .action((key: string, value: string) => {
      if (!isKnownKey(key)) {
        console.error(chalk.red(`Unknown key: ${key}`))
        console.error(chalk.dim('Run `halalchain config list` to see valid keys.'))
        process.exit(1)
      }
      if (key === 'jwt.key') {
        const err = validateJwtKey(value)
        if (err) {
          console.error(chalk.red(err))
          process.exit(1)
        }
      }
      if (key === 'tawheed.jurisdiction' && !isJurisdiction(value)) {
        console.error(chalk.red(`Must be one of: ${JURISDICTIONS.join(', ')}`))
        process.exit(1)
      }
      set(key, value)
      console.log(chalk.green(`✔ ${key} = ${mask(key, value)}`))
    })

  cmd
    .command('unset <key>')
    .description('Remove a key so it falls back to its default')
    .action((key: string) => {
      if (!isKnownKey(key)) {
        console.error(chalk.red(`Unknown key: ${key}`))
        process.exit(1)
      }
      unset(key)
      console.log(chalk.green(`✔ ${key} reset to default`))
    })

  cmd
    .command('list')
    .alias('ls')
    .description('List all config values (secrets masked)')
    .action(() => {
      const all = getAll()
      const width = Math.max(...Object.keys(all).map((k) => k.length))
      console.log(chalk.bold('\n  HalalChain config  ') + chalk.dim(`${CONFIG_PATH}\n`))
      for (const [key, value] of Object.entries(all)) {
        console.log('  ' + chalk.cyan(key.padEnd(width + 2)) + mask(key, value))
      }
      console.log()
    })
  cmd
    .command('validate')
    .description('Check the resolved config for common problems')
    .action(() => {
      const all = getAll()
      const errors: string[] = []

      if (all['jwt.key']) {
        const jwtErr = validateJwtKey(all['jwt.key'])
        if (jwtErr) errors.push(`jwt.key: ${jwtErr}`)
      }

      for (const key of URL_KEYS) {
        const value = all[key]
        if (value && !isValidUrl(value)) errors.push(`${key}: invalid URL "${value}"`)
      }

      if (!isJurisdiction(all['tawheed.jurisdiction'] ?? '')) {
        errors.push(`tawheed.jurisdiction: must be one of ${JURISDICTIONS.join(', ')}`)
      }

      if (!all['mcp.url']) {
        // Not an error: only `halalchain agent` needs MCP, and it reports the
        // gap itself with a more specific message.
        console.log(chalk.dim('  Note: mcp.url is unset — `halalchain agent` will not start.'))
      }

      if (errors.length > 0) {
        console.error(chalk.red(`\n  ${errors.length} issue(s):\n`))
        for (const e of errors) console.error(chalk.red(`    • ${e}`))
        console.error()
        process.exit(1)
      }
      console.log(chalk.green('\n  ✔ Config is valid\n'))
    })

  cmd
    .command('reset')
    .description('Delete the user config file (defaults and env still apply)')
    .action(async () => {
      const ok = await confirm({ message: 'Reset all config to defaults?', default: false })
      if (!ok) {
        console.log('Aborted.')
        return
      }
      reset()
      console.log(chalk.green('✔ Config reset to defaults.'))
    })

  cmd
    .command('path')
    .description('Print the config file path')
    .action(() => {
      console.log(CONFIG_PATH)
    })

  cmd
    .command('keys')
    .description('List valid config keys')
    .action(() => {
      for (const key of KNOWN_KEYS) console.log(key)
    })

  cmd
    .command('show-services')
    .description('Show resolved service URLs and masked API keys')
    .action(() => {
      const all = getAllRedacted()
      console.log(chalk.bold('\nHalalChain service configuration\n'))
      console.log(chalk.cyan('  Service URLs:'))
      for (const [label, key] of [
        ['Platform API', 'platform-api.url'],
        ['Marketplace ', 'marketplace.url'],
        ['Customer UI ', 'halalchain.url'],
        ['AI Gateway  ', 'ai-inference.url'],
        ['Policy Engine', 'tawheed.url'],
        ['Local Models', 'local-models.url'],
        ['Agents      ', 'agents.url'],
      ] as const) {
        console.log(chalk.dim(`    ${label}    ${all[key]}`))
      }
      console.log(chalk.dim(`    MCP           ${mask('mcp.url', all['mcp.url'])}`))
      console.log(chalk.cyan('\n  API keys (masked):'))
      for (const [label, key] of [
        ['AI Gateway  ', 'ai-inference.api-key'],
        ['Policy Engine', 'tawheed.api-key'],
        ['Local Models', 'local-models.api-key'],
      ] as const) {
        console.log(chalk.dim(`    ${label}  ${mask(key, all[key])}`))
      }
      console.log(chalk.cyan('\n  Config file: ') + CONFIG_PATH + '\n')
    })

  return cmd
}

