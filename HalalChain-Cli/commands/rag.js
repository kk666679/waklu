import ora from "ora";
import chalk from "chalk";
import { APIClient } from "../lib/api-client.js";

export class RAGCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const rag = program.command("rag").description("RAG (Retrieval-Augmented Generation)");
    rag.command("query").description("Query RAG pipeline").option("-q, --query <text>", "Query").option("-c, --context <name>", "Context name").option("-j, --json", "JSON").action(this.query.bind(this));
    rag.command("index").description("Index documents").option("-d, --documents <paths>", "Paths").option("-c, --context <name>", "Context name").action(this.index.bind(this));
  }
  async query(options) {
    let query = options.query;
    if (!query) { const { input } = await import("@inquirer/prompts"); query = await input({ message: "Query:", required: true }); }
    const spinner = ora("Querying RAG...").start();
    try {
      const result = await this.api.request("POST", "/rag/query", { query, context_name: options.context, top_k: 5 });
      spinner.succeed("Query complete");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n💡 RAG Response\n"));
      console.log(result.response || result.answer || JSON.stringify(result, null, 2));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
  async index(options) {
    const spinner = ora("Indexing documents...").start();
    try {
      const result = await this.api.request("POST", "/rag/index", { documents: options.documents?.split(",") || [], context_name: options.context });
      spinner.succeed(`Indexed ${result.count || 0} documents`);
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}
