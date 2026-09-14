import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";

export class VectorCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const v = program.command("vector").description("Vector DB operations");
    v.command("search").description("Semantic search").option("-q, --query <text>", "Query").option("-t, --top-k <n>", "Results", parseInt).option("-j, --json", "JSON").action(this.search.bind(this));
    v.command("collections").description("List collections").action(this.collections.bind(this));
    v.command("stats").description("DB statistics").action(this.stats.bind(this));
  }
  async search(options) {
    let query = options.query;
    if (!query) { const { input } = await import("@inquirer/prompts"); query = await input({ message: "Search query:", required: true }); }
    const spinner = ora("Searching...").start();
    try {
      const result = await this.api.request("POST", "/vector/search", { query, top_k: options.topK || 5 });
      spinner.succeed(`Found ${result.results?.length || 0} results`);
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      const table = new Table({ head: ["#", "Score", "Content"], colWidths: [4, 10, 60] });
      (result.results || []).forEach((r, i) => {
        const score = ((r.score || r.similarity || 0) * 100).toFixed(1);
        table.push([i + 1, score + "%", (r.content || r.passage || "").slice(0, 60)]);
      });
      console.log(table.toString());
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
  async collections() {
    try { const r = await this.api.request("GET", "/vector/collections"); console.log(JSON.stringify(r, null, 2)); } catch (e) { console.error(e.message); }
  }
  async stats() {
    try { const r = await this.api.request("GET", "/vector/stats"); console.log(JSON.stringify(r, null, 2)); } catch (e) { console.error(e.message); }
  }
}
