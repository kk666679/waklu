import ora from "ora";
import chalk from "chalk";
import Table from "cli-table3";
import { input, confirm } from "@inquirer/prompts";
import { APIClient } from "../lib/api-client.js";
import { Logger } from "../lib/logger.js";

export class MLCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const ml = program.command("ml").description("ML operations");
    ml.command("train").description("Train model")
      .option("-d, --data <path>", "Training data (JSON file)")
      .option("-t, --type <type>", "Model type", "classification")
      .action(this.train.bind(this));
    ml.command("predict").description("Run prediction")
      .option("-m, --model <name>", "Model name")
      .option("-i, --input <json>", "Input data JSON")
      .action(this.predict.bind(this));
    ml.command("models").description("List models").action(this.listModels.bind(this));
  }
  async train(options) {
    let dataPath = options.data;
    if (!dataPath) {
      dataPath = await input({ message: "Path to training data JSON:" });
    }
    if (!dataPath) {
      Logger.error("No training data path provided");
      process.exit(1);
    }
    const fs = await import("fs-extra");
    let data;
    try {
      data = JSON.parse(await fs.readFile(dataPath, "utf-8"));
    } catch (err) {
      Logger.error(`Could not read training data: ${err.message}`);
      process.exit(1);
    }
    const spinner = ora("Training model...").start();
    try {
      const result = await this.api.request("POST", "/ml/train", { model_type: options.type, data });
      spinner.succeed("Model trained");
      Logger.info(`Accuracy: ${((result.accuracy || 0) * 100).toFixed(2)}%`);
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
  async predict(options) {
    let model = options.model;
    let inputJson = options.input;
    if (!model) model = await input({ message: "Model name:" });
    if (!inputJson) {
      const v = await input({ message: "Input data JSON:" });
      inputJson = v;
    }
    let parsed;
    try {
      parsed = JSON.parse(inputJson || "{}");
    } catch (err) {
      Logger.error(`Input is not valid JSON: ${err.message}`);
      process.exit(1);
    }
    const spinner = ora("Predicting...").start();
    try {
      const result = await this.api.request("POST", "/ml/predict", { model_name: model, data: parsed });
      spinner.succeed("Prediction complete");
      console.log(JSON.stringify(result, null, 2));
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
  async listModels() {
    try { const r = await this.api.request("GET", "/ml/models"); console.log(JSON.stringify(r, null, 2)); } catch (e) { Logger.error(e.message); }
  }
}
