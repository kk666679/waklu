import { execFile } from 'node:child_process'
import { promisify } from 'node:util'

const execFileAsync = promisify(execFile)

export interface IntentResult {
  stdout: string
  stderr: string
}

/**
 * Thin wrapper around @tanstack/intent. The CLI does not reimplement skill
 * discovery — Intent owns it. This exists so commands can trigger validation
 * or installation without shelling out manually.
 */
export async function runIntent(
  args: string[],
  cwd: string = process.cwd()
): Promise<IntentResult> {
  const { stdout, stderr } = await execFileAsync(
    'npx',
    ['@tanstack/intent@latest', ...args],
    { cwd, env: process.env }
  )
  return { stdout, stderr }
}

export async function listSkills(cwd?: string): Promise<IntentResult> {
  return runIntent(['list'], cwd)
}

export async function validateSkills(cwd?: string): Promise<IntentResult> {
  return runIntent(['validate'], cwd)
}

export async function installSkills(cwd?: string): Promise<IntentResult> {
  return runIntent(['install', '--map'], cwd)
}
