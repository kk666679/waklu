import ora from "ora";
import chalk from "chalk";
import { APIClient } from "../lib/api-client.js";

export class SummarizeCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const cmd = program.command("summarize").description("Text summarization");
    cmd.command("generate")
      .description("Generate summary")
      .option("-t, --text <text>", "Text to summarize")
      .option("-f, --file <path>", "Read text from file")
      .option("-m, --max-tokens <n>", "Max tokens", parseInt)
      .option("-j, --json", "Output as JSON")
      .action(this.generate.bind(this));
  }

  async generate(options) {
    let text = options.text;
    if (options.file) { const fs = await import("fs-extra"); text = await fs.readFile(options.file, "utf-8"); }
    if (!text) { const { input } = await import("@inquirer/prompts"); text = await input({ message: "Enter text to summarize:", required: true }); }
    const spinner = ora("Summarizing...").start();
    try {
      const result = await this.api.summarize(text, options.maxTokens || 80);
      spinner.succeed("Summary generated");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n📝 Summary\n"));
      console.log(chalk.white(result.summary));
      console.log(chalk.dim(`\nModel: ${result.model} | Tokens: ~${result.summary.split(/\s+/).length}`));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}