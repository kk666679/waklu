import ora from "ora";
import chalk from "chalk";
import { APIClient } from "../lib/api-client.js";

export class SummarizeCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    program.command("summarize")
      .description("Summarize text")
      .option("-t, --text <text>", "Text to summarize")
      .option("-f, --file <path>", "Read text from file")
      .option("-m, --max-tokens <n>", "Max summary tokens", parseInt)
      .option("-j, --json", "Output as JSON")
      .action(this.run.bind(this));
  }
  async run(options) {
    let text = options.text;
    if (options.file) { const fs = await import("fs-extra"); text = await fs.readFile(options.file, "utf-8"); }
    if (!text) { const { input } = await import("@inquirer/prompts"); text = await input({ message: "Text to summarize:", required: true }); }
    const spinner = ora("Summarizing...").start();
    try {
      const result = await this.api.request("POST", "/summarize", { text, max_tokens: options.maxTokens || 80 });
      spinner.succeed("Summary generated");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n📝 Summary\n"));
      console.log(chalk.white(result.summary));
      console.log(chalk.gray(`\nModel: ${result.model} | Cached: ${result.cached || false}`));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}
