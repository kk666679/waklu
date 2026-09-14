import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";

export class EvaluateCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const ev = program.command("evaluate").alias("eval").description("Evaluate AI/ML performance");
    ev.command("model").description("Evaluate model").option("-m, --model <name>", "Model").option("-d, --data <path>", "Test data").action(this.model.bind(this));
    ev.command("rag").description("Evaluate RAG").option("-q, --questions <path>", "Questions file").action(this.rag.bind(this));
  }
  async model(options) {
    const spinner = ora("Evaluating...").start();
    try {
      const fs = await import("fs-extra");
      const data = options.data ? JSON.parse(await fs.readFile(options.data, "utf-8")) : [];
      const r = await this.api.request("POST", "/evaluate/model", { model_name: options.model, test_data: data });
      spinner.succeed("Evaluation complete");
      const table = new Table({ head: ["Metric", "Value"], colWidths: [25, 15] });
      Object.entries(r.metrics || {}).forEach(([k, v]) => table.push([k, typeof v === "number" ? v.toFixed(4) : v]));
      console.log(table.toString());
    } catch (e) { spinner.fail(e.message); }
  }
  async rag(options) {
    const spinner = ora("Evaluating RAG...").start();
    try {
      const r = await this.api.request("POST", "/evaluate/rag", { questions: options.questions || [] });
      spinner.succeed("RAG evaluation complete");
      console.log(JSON.stringify(r, null, 2));
    } catch (e) { spinner.fail(e.message); }
  }
}
