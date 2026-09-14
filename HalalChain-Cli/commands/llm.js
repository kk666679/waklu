import ora from "ora";
import chalk from "chalk";
import boxen from "boxen";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";

export class LLMCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const llm = program.command("llm").description("LLM operations");
    llm.command("chat").description("Chat with LLM")
      .option("-p, --prompt <text>", "Prompt text").option("-f, --file <path>", "Read from file")
      .option("-m, --model <model>", "Model").option("-t, --temperature <n>", "Temperature", parseFloat)
      .option("-j, --json", "JSON output").action(this.chat.bind(this));
    llm.command("complete").description("Text completion")
      .option("-p, --prompt <text>", "Prompt").option("-f, --file <path>", "Read from file")
      .option("-j, --json", "JSON output").action(this.complete.bind(this));
    llm.command("models").description("List available models").action(this.models.bind(this));
  }
  async chat(options) {
    let prompt = options.prompt;
    if (options.file) { const fs = await import("fs-extra"); prompt = await fs.readFile(options.file, "utf-8"); }
    if (!prompt) { const { input } = await import("@inquirer/prompts"); prompt = await input({ message: "Enter prompt:", required: true }); }
    const spinner = ora("Chatting with LLM...").start();
    try {
      const result = await this.api.request("POST", "/llm/chat", {
        messages: [{ role: "user", content: prompt }],
        model: options.model || "gpt-3.5-turbo",
        temperature: options.temperature || 0.7,
      });
      spinner.succeed("Response received");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log("\n" + boxen(result.response || result.content || result.text || JSON.stringify(result), { padding: 1, borderStyle: "round", borderColor: "blue" }));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
  async complete(options) {
    let prompt = options.prompt;
    if (options.file) { const fs = await import("fs-extra"); prompt = await fs.readFile(options.file, "utf-8"); }
    if (!prompt) { const { input } = await import("@inquirer/prompts"); prompt = await input({ message: "Enter prompt:", required: true }); }
    const spinner = ora("Generating...").start();
    try {
      const result = await this.api.request("POST", "/llm/complete", { prompt, max_tokens: 200 });
      spinner.succeed("Done");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.white(result.response || result.text || JSON.stringify(result)));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
  async models() {
    const spinner = ora("Fetching models...").start();
    try {
      const result = await this.api.request("GET", "/llm/models");
      spinner.succeed("Models retrieved");
      const table = new Table({ head: ["Provider", "Model", "Context"], colWidths: [15, 30, 15] });
      (result.models || []).forEach(m => table.push([m.provider || "?", m.name, m.context_length || "N/A"]));
      console.log(table.toString());
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}
