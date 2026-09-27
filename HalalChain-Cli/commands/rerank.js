import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";

export class RerankCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const cmd = program.command("rerank").description("Rerank passages by relevance");
    cmd.command("score")
      .description("Rerank passages for a query")
      .option("-q, --query <text>", "Query text")
      .option("-p, --passages <passages...>", "Passages to rerank")
      .option("-f, --file <path>", "Read passages from file (JSON array)")
      .option("-j, --json", "Output as JSON")
      .action(this.score.bind(this));
  }

  async score(options) {
    let query = options.query;
    let passages = options.passages || [];
    if (options.file) { const fs = await import("fs-extra"); passages = JSON.parse(await fs.readFile(options.file, "utf-8")); }
    if (!query) { const { input } = await import("@inquirer/prompts"); query = await input({ message: "Enter query:", required: true }); }
    if (!passages.length) { const { input } = await import("@inquirer/prompts"); const p = await input({ message: "Passages (comma-separated):", required: true }); passages = p.split(",").map(s => s.trim()); }
    const spinner = ora("Reranking...").start();
    try {
      const result = await this.api.rerank(query, passages);
      spinner.succeed("Reranking complete");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n🔄 Rerank Results\n"));
      console.log(chalk.bold("Query:"), query.slice(0, 80));
      const table = new Table({ head: ["Rank", "Score", "Passage"], colWidths: [6, 8, 60] });
      result.results.forEach((r, i) => {
        table.push([i + 1, (r.score * 100).toFixed(1) + "%", r.passage.slice(0, 80)]);
      });
      console.log(table.toString());
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}