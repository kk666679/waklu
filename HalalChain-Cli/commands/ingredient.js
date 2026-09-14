import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";

export class IngredientCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    program.command("ingredient")
      .description("Parse and analyze ingredients for halal compliance")
      .option("-t, --text <text>", "Ingredient list text")
      .option("-f, --file <path>", "Read from file")
      .option("-j, --json", "Output as JSON")
      .action(this.run.bind(this));
  }
  async run(options) {
    let text = options.text;
    if (options.file) { const fs = await import("fs-extra"); text = await fs.readFile(options.file, "utf-8"); }
    if (!text) { const { input } = await import("@inquirer/prompts"); text = await input({ message: "Ingredient list:", required: true }); }
    const spinner = ora("Parsing ingredients...").start();
    try {
      const result = await this.api.request("POST", "/ingredient-parse", { text });
      spinner.succeed("Analysis complete");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n🧪 Ingredient Analysis\n"));
      const table = new Table({ head: ["Ingredient", "E-Code", "Risk"], colWidths: [30, 10, 12] });
      for (const item of result.parsed || []) {
        const risk = item.risk === "haram" ? chalk.red("HARAM") : item.risk === "mashbooh" ? chalk.yellow("MASHBOOH") : chalk.green("HALAL");
        table.push([item.name, item.eCode || "-", risk]);
      }
      console.log(table.toString());
      const s = result.summary || {};
      console.log(chalk.bold(`\nTotal: ${s.total} | Halal: ${chalk.green(s.halal)} | Haram: ${chalk.red(s.haram)} | Mashbooh: ${chalk.yellow(s.mashbooh)}`));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}
