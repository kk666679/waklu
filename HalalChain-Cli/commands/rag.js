import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";

export class RAGCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const cmd = program.command("rag").description("RAG operations");
    cmd.command("add")
      .description("Add documents to vector store")
      .option("-f, --file <path>", "JSON file with documents")
      .option("-c, --content <text>", "Single document content")
      .option("-s, --source <url>", "Document source")
      .action(this.add.bind(this));
    cmd.command("search")
      .description("Search vector store")
      .option("-q, --query <text>", "Search query")
      .option("-k, --top-k <n>", "Top K results", parseInt)
      .option("-j, --json", "Output as JSON")
      .action(this.search.bind(this));
    cmd.command("clear")
      .description("Clear vector store")
      .action(this.clear.bind(this));
  }

  async add(options) {
    let documents = [];
    if (options.file) { const fs = await import("fs-extra"); documents = JSON.parse(await fs.readFile(options.file, "utf-8")); }
    else if (options.content) { documents = [{ content: options.content, source: options.source || "cli" }]; }
    else { const { input } = await import("@inquirer/prompts"); const c = await input({ message: "Document content:", required: true }); documents = [{ content: c, source: "cli" }]; }
    const spinner = ora("Adding documents...").start();
    try {
      const result = await this.api.ragAddDocuments(documents);
      spinner.succeed("Documents added");
      console.log(chalk.green(`  Added ${result.chunks_added} chunks`));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }

  async search(options) {
    let query = options.query;
    if (!query) { const { input } = await import("@inquirer/prompts"); query = await input({ message: "Search query:", required: true }); }
    const spinner = ora("Searching...").start();
    try {
      const result = await this.api.ragSearch(query, options.topK || 5);
      spinner.succeed("Search complete");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n🔍 RAG Search\n"));
      console.log(chalk.bold("Query:"), query);
      const table = new Table({ head: ["#", "Score", "Source", "Preview"], colWidths: [4, 8, 20, 50] });
      result.results.forEach((r, i) => {
        table.push([i + 1, (r.score * 100).toFixed(1) + "%", r.metadata?.source || "unknown", r.content.slice(0, 80)]);
      });
      console.log(table.toString());
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }

  async clear() {
    const { confirm } = await import("@inquirer/prompts");
    const ok = await confirm({ message: "Clear all documents from vector store?", default: false });
    if (!ok) { console.log("Aborted."); return; }
    const spinner = ora("Clearing...").start();
    try {
      await this.api.ragClear();
      spinner.succeed("Vector store cleared");
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}