import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";

export class PipelineCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const p = program.command("pipeline").description("AI/ML pipeline operations");
    p.command("run").description("Run pipeline").option("-f, --file <path>", "Config file").action(this.run.bind(this));
    p.command("list").description("List pipelines").action(this.list.bind(this));
  }
  async run(options) {
    let config = {};
    if (options.file) { const fs = await import("fs-extra"); config = JSON.parse(await fs.readFile(options.file, "utf-8")); }
    const spinner = ora("Running pipeline...").start();
    try {
      const result = await this.api.request("POST", "/pipeline/run", { pipeline: config });
      spinner.succeed("Pipeline completed");
      if (result.summary) console.log(chalk.bold("Steps:"), result.summary.total_steps, chalk.bold("Success:"), result.summary.successful);
    } catch (e) { spinner.fail(e.message); }
  }
  async list() {
    try { const r = await this.api.request("GET", "/pipeline/list"); console.log(JSON.stringify(r, null, 2)); } catch (e) { console.error(e.message); }
  }
}
