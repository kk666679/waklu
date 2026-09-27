import ora from "ora";
import chalk from "chalk";
import { APIClient } from "../lib/api-client.js";

export class BatchCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const cmd = program.command("batch").description("Batch processing");
    cmd.command("embeddings")
      .description("Generate embeddings for multiple texts")
      .option("-f, --file <path>", "JSON file with texts array")
      .option("-o, --output <path>", "Output file")
      .action(this.embeddings.bind(this));
  }

  async embeddings(options) {
    let texts = [];
    if (options.file) { const fs = await import("fs-extra"); texts = JSON.parse(await fs.readFile(options.file, "utf-8")); }
    else { const { input } = await import("@inquirer/prompts"); const t = await input({ message: "Texts (comma-separated):", required: true }); texts = t.split(",").map(s => s.trim()); }
    const spinner = ora(`Generating ${texts.length} embeddings...`).start();
    try {
      const results = [];
      for (const text of texts) {
        const result = await this.api.embeddings(text);
        results.push({ text, embedding: result.embedding });
      }
      spinner.succeed("Batch complete");
      if (options.output) { const fs = await import("fs-extra"); await fs.writeJson(options.output, results, { spaces: 2 }); console.log(chalk.green(`Saved to ${options.output}`)); }
      console.log(chalk.cyan(`\n  Generated ${results.length} embeddings`));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}