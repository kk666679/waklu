#!/usr/bin/env node
import { Command } from 'commander'
import { pathToFileURL } from 'node:url'
import { registerMCPCleanup } from './lib/mcp-client.js'
import { logger } from './lib/logger.js'
import { get } from './lib/config.js'

import { createAgentCommand } from './commands/agent.js'
import { createConfigCommand } from './commands/config.js'
import { createEnvCommand } from './commands/env.js'
import { createServerCommand } from './commands/server.js'
import { createEmbeddingCommand } from './commands/embedding.js'
import { createClassifyCommand } from './commands/classify.js'
import { createSummarizeCommand } from './commands/summarize.js'
import { createRerankCommand } from './commands/rerank.js'
import { createRagCommand } from './commands/rag.js'
import { createVectorCommand } from './commands/vector.js'
import { createCertificateCommand } from './commands/certificate.js'
import { createIngredientCommand } from './commands/ingredient.js'
import { createLlmCommand } from './commands/llm.js'
import { createEvaluateCommand } from './commands/evaluate.js'
import { createAgentsCommand } from './commands/agents.js'
import { createLocalCommand } from './commands/local.js'
import { createSandboxCommand } from './commands/sandbox.js'
import { validateSkills, listSkills, installSkills, runIntent } from './runtime/intent.js'

registerMCPCleanup()

/** Build the program. Exported so tests can assert on the registered surface. */
export function buildProgram(): Command {
  const program = new Command()
    .name('halalchain')
    .description('HalalChain operator CLI — queries, evidence, and policy verdicts over MCP and HTTP')
    .version('3.0.0')
    .option('-v, --verbose', 'verbose output on stderr')
    .option('--no-color', 'disable ANSI colour')
    .hook('preAction', (thisCommand) => {
      if (thisCommand.opts().verbose || get('verbose', 'false') === 'true') {
        logger.setLevel('debug')
      }
    })
    .showHelpAfterError('(run `halalchain --help` for usage)')

  program.addCommand(createAgentCommand())
  program.addCommand(createConfigCommand())
  program.addCommand(createEnvCommand())
  program.addCommand(createServerCommand())
  program.addCommand(createEmbeddingCommand())
  program.addCommand(createClassifyCommand())
  program.addCommand(createSummarizeCommand())
  program.addCommand(createRerankCommand())
  program.addCommand(createRagCommand())
  program.addCommand(createVectorCommand())
  program.addCommand(createCertificateCommand())
  program.addCommand(createIngredientCommand())
  program.addCommand(createLlmCommand())
  program.addCommand(createEvaluateCommand())
  program.addCommand(createAgentsCommand())
  program.addCommand(createLocalCommand())
  program.addCommand(createSandboxCommand())

  const skills = new Command('skills')
    .description('Inspect and install the CLI skill definitions (skills/*.md)')
  skills.command('list').description('List discovered skills').action(async () => {
    process.stdout.write((await listSkills(process.cwd())).stdout)
  })
  skills.command('validate').description('Validate skill definitions').action(async () => {
    const result = await validateSkills(process.cwd())
    process.stdout.write(result.stdout)
    process.stderr.write(result.stderr)
  })
  skills.command('install').description('Install skills into your agent config').action(async () => {
    process.stdout.write((await installSkills(process.cwd())).stdout)
  })
  skills.command('run <args...>').description('Pass arguments straight to @tanstack/intent').action(
    async (args: string[]) => {
      const result = await runIntent(args, process.cwd())
      process.stdout.write(result.stdout)
      process.stderr.write(result.stderr)
    }
  )
  program.addCommand(skills)

  return program
}

const program = buildProgram()

// Only drive argv when this module is the process entry point. Tests import
// `buildProgram` to inspect the registered command surface, and running
// parseAsync on import would try to interpret the test runner's own arguments.
const invokedDirectly =
  process.argv[1] !== undefined &&
  import.meta.url === pathToFileURL(process.argv[1]).href

if (invokedDirectly) {
  program.parseAsync(process.argv).catch((err) => {
    logger.error(err instanceof Error ? err.message : String(err))
    process.exit(1)
  })
}

export { program }


