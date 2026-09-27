import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";

export class IngredientCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const cmd = program.command("ingredient").description("Ingredient parsing & analysis");
    cmd.command("parse")
      .description("Parse ingredient list")
      .option("-t, --text <text>", "Ingredient text")
      .option("-f, --file <path>", "Read from file")
      .option("-j, --json", "Output as JSON")
      .action(this.parse.bind(this));
  }

  async parse(options) {
    let text = options.text;
    if (options.file) { const fs = await import("fs-extra"); text = await fs.readFile(options.file, "utf-8"); }
    if (!text) { const { input } = await import("@inquirer/prompts"); text = await input({ message: "Enter ingredient list:", required: true }); }
    const spinner = ora("Parsing ingredients...").start();
    try {
      const result = await this.api.ingredientParse(text);
      spinner.succeed("Ingredients parsed");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n🧪 Ingredient Analysis\n"));
      const table = new Table({ head: ["#", "Ingredient", "Status", "Confidence"], colWidths: [4, 30, 12, 10] });
      result.parsed.forEach((item, i) => {
        const statusColor = item.status === "halal" ? "green" : item.status === "haram" ? "red" : "yellow";
        table.push([i + 1, item.name, chalk[statusColor](item.status), (item.confidence * 100).toFixed(0) + "%"]);
      });
      console.log(table.toString());
      console.log(chalk.dim("\n" + result.summary));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}