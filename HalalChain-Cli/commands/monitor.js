import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { APIClient } from "../lib/api-client.js";
import { Logger } from "../lib/logger.js";

export class MonitorCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    program.command("monitor").description("Monitor AI service metrics").action(this.run.bind(this));
  }
  async run() {
    const spinner = ora("Fetching metrics...").start();
    try {
      // /health is currently a POST in the service for legacy reasons, but
      // the documented healthcheck endpoints are GET /health/live and
      // GET /health/ready. Use the documented one.
      const [health, metrics] = await Promise.all([
        this.api.request("GET", "/health/ready").catch((err) => ({ error: err.message })),
        this.api.request("GET", "/metrics").catch(() => null),
      ]);
      spinner.succeed("Metrics retrieved");
      Logger.info(`\nService Monitor`);
      Logger.info(`Service: ${health.service || "n/a"}`);
      Logger.info(`Status: ${health.status || "n/a"}`);
      if (health.dependencies) {
        Logger.info(`Dependencies: ${JSON.stringify(health.dependencies)}`);
      }
      if (health.error) {
        Logger.warn(`Health check error: ${health.error}`);
      }
      if (metrics) {
        Logger.info(`Metrics: ${JSON.stringify(metrics)}`);
      }
    } catch (e) { spinner.fail(e.message); }
  }
}
