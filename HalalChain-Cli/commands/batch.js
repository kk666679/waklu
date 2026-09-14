import ora from "ora";
import chalk from "chalk";
import { APIClient } from "../lib/api-client.js";

export class BatchCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    program.command("batch")
      .description("Batch process multiple items")
      .option("-f, --file <path>", "Input JSON file")
      .option("-o, --operation <op>", "Operation: classify, embed, summarize")
      .option("-j, --json", "JSON output")
      .action(this.run.bind(this));
  }
  async run(options) {
    if (!options.file) { console.error("Specify -f <file>"); return; }
    const fs = await import("fs-extra");
    const data = JSON.parse(await fs.readFile(options.file, "utf-8"));
    const items = Array.isArray(data) ? data : [data];
    const spinner = ora(`Processing ${items.length} items...`).start();
    try {
      const results = await this.api.request("POST", "/batch", { operation: options.operation || "classify", items });
      spinner.succeed(`Processed ${results.results?.length || items.length} items`);
      if (options.json) { console.log(JSON.stringify(results, null, 2)); return; }
      console.log(chalk.cyan("\n📦 Batch Results\n"));
      (results.results || []).forEach((r, i) => console.log(`  ${i + 1}. ${JSON.stringify(r).slice(0, 80)}`));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}
