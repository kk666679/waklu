import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'node:fs'
import { homedir } from 'node:os'
import { join, dirname } from 'node:path'
import { parse, stringify } from 'yaml'

export interface CliConfig {
  mcpUrl?: string
  platformApiUrl?: string
  defaultModel?: string
  verbose?: boolean
}

const CONFIG_PATH = join(homedir(), '.halalchain', 'cli.yaml')

export function loadConfig(): CliConfig {
  if (!existsSync(CONFIG_PATH)) return {}
  return parse(readFileSync(CONFIG_PATH, 'utf8')) as CliConfig
}

export function saveConfig(config: CliConfig): void {
  mkdirSync(dirname(CONFIG_PATH), { recursive: true })
  writeFileSync(CONFIG_PATH, stringify(config), 'utf8')
}

export function resolveConfig(): Required<CliConfig> {
  const stored = loadConfig()
  return {
    mcpUrl: stored.mcpUrl ?? process.env.HALALCHAIN_MCP_URL ?? '',
    platformApiUrl: stored.platformApiUrl ?? process.env.HALALCHAIN_API_URL ?? '',
    defaultModel: stored.defaultModel ?? 'gpt-5.2',
    verbose: stored.verbose ?? false,
  }
}
