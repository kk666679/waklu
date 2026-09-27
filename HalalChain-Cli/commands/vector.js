import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";

export class VectorCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const cmd = program.command("vector").description("Vector store operations");
    cmd.command("stats")
      .description("Show vector store statistics")
      .action(this.stats.bind(this));
  }

  async stats() {
    const spinner = ora("Fetching vector store stats...").start();
    try {
      const result = await this.api.request("GET", "/health/ready");
      spinner.succeed("Stats retrieved");
      console.log(chalk.cyan("\n📊 Vector Store Stats\n"));
      console.log(chalk.bold("Status:"), result.status);
      console.log(chalk.bold("Dependencies:"), JSON.stringify(result.dependencies, null, 2));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}