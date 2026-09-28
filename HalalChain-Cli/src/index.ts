#!/usr/bin/env node
import { Command } from 'commander'
import { registerMCPCleanup } from './lib/mcp-client.js'
import { logger } from './lib/logger.js'
import { agentCommand } from './commands/agent.js'

registerMCPCleanup()

const program = new Command()
  .name('halalchain')
  .version('3.0.0')
  .description('HalalChain operator CLI')
  .option('-v, --verbose', 'verbose output')
  .hook('preAction', (thisCommand) => {
    if (thisCommand.opts().verbose) logger.setLevel('debug')
  })

program.addCommand(agentCommand)

program.parseAsync(process.argv).catch((err) => {
  logger.error(err instanceof Error ? err.message : String(err))
  process.exit(1)
})
