import ora from "ora";
import chalk from "chalk";
import { APIClient } from "../lib/api-client.js";

export class ClassifyCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const cmd = program.command("classify").description("Text classification");
    cmd.command("halal")
      .description("Classify text for halal/haram/mashbooh")
      .option("-t, --text <text>", "Text to classify")
      .option("-f, --file <path>", "Read text from file")
      .option("-j, --json", "Output as JSON")
      .action(this.halal.bind(this));
    cmd.command("generic")
      .description("Generic multi-label classification")
      .option("-t, --text <text>", "Text to classify")
      .option("-l, --labels <labels>", "Comma-separated labels")
      .option("-f, --file <path>", "Read text from file")
      .option("-j, --json", "Output as JSON")
      .action(this.generic.bind(this));
  }

  async halal(options) {
    let text = options.text;
    if (options.file) { const fs = await import("fs-extra"); text = await fs.readFile(options.file, "utf-8"); }
    if (!text) { const { input } = await import("@inquirer/prompts"); text = await input({ message: "Enter text to classify:", required: true }); }
    const spinner = ora("Classifying...").start();
    try {
      const result = await this.api.classify(text, ["halal", "haram", "mashbooh", "unknown"]);
      spinner.succeed("Classification complete");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n🕌 Halal Classification\n"));
      console.log(chalk.bold("Best Label:"), chalk.green(result.bestLabel));
      console.log(chalk.bold("Confidence:"), (result.bestScore * 100).toFixed(1) + "%");
      console.log(chalk.bold("Scores:"));
      for (const [label, score] of Object.entries(result.scores)) {
        const bar = "█".repeat(Math.round(score * 20));
        console.log(`  ${label.padEnd(10)} ${chalk.cyan(bar)} ${(score * 100).toFixed(1)}%`);
      }
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }

  async generic(options) {
    let text = options.text;
    let labels = options.labels?.split(",").map(s => s.trim()) || [];
    if (options.file) { const fs = await import("fs-extra"); text = await fs.readFile(options.file, "utf-8"); }
    if (!text) { const { input } = await import("@inquirer/prompts"); text = await input({ message: "Enter text:", required: true }); }
    if (!labels.length) { const { input } = await import("@inquirer/prompts"); const l = await input({ message: "Labels (comma-separated):", required: true }); labels = l.split(",").map(s => s.trim()); }
    const spinner = ora("Classifying...").start();
    try {
      const result = await this.api.classify(text, labels);
      spinner.succeed("Classification complete");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n📋 Classification\n"));
      console.log(chalk.bold("Best:"), chalk.green(result.bestLabel), `(${result.bestScore.toFixed(2)})`);
      for (const [label, score] of Object.entries(result.scores)) {
        console.log(`  ${label}: ${(score * 100).toFixed(1)}%`);
      }
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}