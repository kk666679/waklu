import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";
import { Logger } from "../lib/logger.js";

export class EmbeddingCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const cmd = program.command("embedding").description("Embedding generation");

    cmd.command("generate")
      .description("Generate embedding for text")
      .option("-t, --text <text>", "Text to embed")
      .option("-f, --file <path>", "Read text from file")
      .option("-j, --json", "Output as JSON")
      .action(this.generate.bind(this));

    cmd.command("compare")
      .description("Compare similarity between two texts")
      .option("-a, --text-a <text>", "First text")
      .option("-b, --text-b <text>", "Second text")
      .option("-j, --json", "Output as JSON")
      .action(this.compare.bind(this));
  }

  async generate(options) {
    let text = options.text;
    if (options.file) {
      const fs = await import("fs-extra");
      text = await fs.readFile(options.file, "utf-8");
    }
    if (!text) {
      const { input } = await import("@inquirer/prompts");
      text = await input({ message: "Enter text to embed:", required: true });
    }
    const spinner = ora("Generating embedding...").start();
    try {
      const result = await this.api.request("POST", "/embeddings", { text });
      spinner.succeed("Embedding generated");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n🔢 Embedding\n"));
      console.log(chalk.bold("Model:"), result.model);
      console.log(chalk.bold("Dimension:"), result.embedding?.length || "N/A");
      console.log(chalk.bold("Preview:"), result.embedding?.slice(0, 8).map(v => v.toFixed(4)).join(", "), "...");
      console.log(chalk.bold("Cached:"), result.cached || false);
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }

  async compare(options) {
    let a = options.textA, b = options.textB;
    if (!a || !b) {
      const { input } = await import("@inquirer/prompts");
      a = a || await input({ message: "First text:", required: true });
      b = b || await input({ message: "Second text:", required: true });
    }
    const spinner = ora("Comparing embeddings...").start();
    try {
      const [embA, embB] = await Promise.all([
        this.api.request("POST", "/embeddings", { text: a }),
        this.api.request("POST", "/embeddings", { text: b }),
      ]);
      const sim = cosineSim(embA.embedding, embB.embedding);
      spinner.succeed("Comparison complete");
      if (options.json) { console.log(JSON.stringify({ similarity: sim, textA: a, textB: b }, null, 2)); return; }
      console.log(chalk.cyan("\n📊 Similarity\n"));
      console.log(chalk.bold("Text A:"), a.slice(0, 60));
      console.log(chalk.bold("Text B:"), b.slice(0, 60));
      const pct = (sim * 100).toFixed(1);
      const color = sim > 0.7 ? "green" : sim > 0.4 ? "yellow" : "red";
      console.log(chalk.bold("Similarity:"), chalk[color](pct + "%"));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}

function cosineSim(a, b) {
  if (!a || !b || a.length !== b.length) return 0;
  let dot = 0, magA = 0, magB = 0;
  for (let i = 0; i < a.length; i++) { dot += a[i] * b[i]; magA += a[i] * a[i]; magB += b[i] * b[i]; }
  return dot / (Math.sqrt(magA) * Math.sqrt(magB) || 1);
}
