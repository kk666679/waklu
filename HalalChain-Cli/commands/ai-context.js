import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { input } from "@inquirer/prompts";
import { APIClient } from "../lib/api-client.js";
import { Logger } from "../lib/logger.js";

export class AIContextCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const ai = program.command("ai-context").alias("aic").description("AI Context management");
    ai.command("create").description("Create context from documents")
      .option("-d, --documents <paths>", "Document paths")
      .option("-c, --context <name>", "Context name (alias for --name, avoids -n/--name collision with other commands)")
      .option("-n, --name <name>", "Context name (alias for --context)")
      .action(this.create.bind(this));
    ai.command("query").description("Query context")
      .option("-c, --context <name>", "Context name")
      .option("-q, --query <text>", "Query")
      .action(this.query.bind(this));
    ai.command("list").description("List contexts").action(this.list.bind(this));
    ai.command("similarity").description("Find similar content")
      .option("-t, --text <text>", "Text")
      .option("-c, --context <name>", "Context name")
      .action(this.similarity.bind(this));
  }
  async create(options) {
    const name = options.name || options.context || `context_${Date.now()}`;
    const spinner = ora(`Creating context '${name}'...`).start();
    try {
      const result = await this.api.request("POST", "/ai-context/create", {
        name,
        documents: options.documents?.split(",") || [],
      });
      spinner.succeed(`Context '${name}' created`);
      Logger.info(`Documents: ${result.document_count ?? "?"}, Chunks: ${result.chunk_count ?? "?"}`);
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
  async query(options) {
    let name = options.context;
    let query = options.query;
    if (!name) name = await input({ message: "Context name:" });
    if (!query) query = await input({ message: "Query:" });
    const spinner = ora("Querying...").start();
    try {
      const result = await this.api.request("POST", "/ai-context/query", { context_name: name, query, top_k: 5 });
      spinner.succeed("Query complete");
      Logger.info(`\nResults`);
      (result.results || []).forEach((r, i) => Logger.info(`${i + 1}. [${((r.score || 0) * 100).toFixed(0)}%] ${(r.content || "").slice(0, 80)}`));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
  async list() {
    try { const r = await this.api.request("GET", "/ai-context/list"); console.log(JSON.stringify(r, null, 2)); } catch (e) { Logger.error(e.message); }
  }
  async similarity(options) {
    let text = options.text;
    let name = options.context;
    if (!text) text = await input({ message: "Reference text:" });
    if (!name) name = await input({ message: "Context name:" });
    const spinner = ora("Finding similar...").start();
    try {
      const r = await this.api.request("POST", "/ai-context/similarity", { text, context_name: name, top_k: 5 });
      spinner.succeed("Done");
      (r.results || []).forEach((item, i) => Logger.info(`${i + 1}. [${((item.similarity || 0) * 100).toFixed(0)}%] ${(item.content || "").slice(0, 80)}`));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}
