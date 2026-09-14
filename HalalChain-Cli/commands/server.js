import ora from "ora";
import chalk from "chalk";
import boxen from "boxen";

export class ServerCommand {
  constructor(program, configManager) {
    this.config = configManager;
    const srv = program.command("server").description("Server management");
    srv.command("status").description("Check server status").action(this.status.bind(this));
    srv.command("start").description("Start ai-inference server").action(this.start.bind(this));
  }
  async status() {
    const spinner = ora("Checking server...").start();
    try {
      const url = this.config.get("ai-inference.url", "http://localhost:7071");
      const r = await fetch(url + "/health", { method: "POST", headers: { "content-type": "application/json" }, body: "{}", signal: AbortSignal.timeout(5000) });
      const data = await r.json();
      spinner.succeed("Server is healthy");
      console.log(boxen(JSON.stringify(data, null, 2), { padding: 1, borderColor: "green" }));
    } catch (e) { spinner.fail("Server unreachable: " + e.message); }
  }
  async start() {
    console.log(chalk.cyan("Starting ai-inference server..."));
    console.log(chalk.dim("Run: cd ai-inference && python -m uvicorn src.main:app --host 0.0.0.0 --port 7071"));
  }
}
