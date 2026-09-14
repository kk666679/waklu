import ora from "ora";
import chalk from "chalk";
import { APIClient } from "../lib/api-client.js";

export class ClassifyCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    program.command("classify")
      .description("Classify text with halal/generic labels")
      .option("-t, --text <text>", "Text to classify")
      .option("-f, --file <path>", "Read text from file")
      .option("-l, --labels <labels>", "Comma-separated labels", "halal,haram,mashbooh")
      .option("-j, --json", "Output as JSON")
      .action(this.run.bind(this));
  }

  async run(options) {
    let text = options.text;
    if (options.file) { const fs = await import("fs-extra"); text = await fs.readFile(options.file, "utf-8"); }
    if (!text) { const { input } = await import("@inquirer/prompts"); text = await input({ message: "Text to classify:", required: true }); }
    const labels = options.labels.split(",").map(l => l.trim());
    const spinner = ora("Classifying...").start();
    try {
      const result = await this.api.request("POST", "/classify", { text, labels });
      spinner.succeed("Classification complete");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n🏷️  Classification\n"));
      console.log(chalk.bold("Best:"), chalk[result.bestLabel === "haram" ? "red" : result.bestLabel === "halal" ? "green" : "yellow"](result.bestLabel), `(${(result.bestScore * 100).toFixed(1)}%)`);
      console.log(chalk.bold("Scores:"));
      for (const [label, score] of Object.entries(result.scores || {})) {
        const bar = "█".repeat(Math.round(score * 20)) + "░".repeat(20 - Math.round(score * 20));
        console.log(`  ${label.padEnd(12)} ${bar} ${(score * 100).toFixed(1)}%`);
      }
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}
