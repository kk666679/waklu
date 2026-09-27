import ora from "ora";
import chalk from "chalk";
import { APIClient, createTawheedClient, createLocalModelsClient } from "../lib/api-client.js";
import { Logger } from "../lib/logger.js";

export class CrossServiceCommand {
  constructor(program, configManager) {
    this.aiClient = new APIClient(configManager);
    this.tawheedClient = createTawheedClient(configManager);
    this.localModelsClient = createLocalModelsClient(configManager);
    this.setupCommands(program);
  }

  setupCommands(program) {
    const cmd = program.command("cross").description("Cross-service operations");

    cmd.command("health")
      .description("Check health of all services")
      .action(this.health.bind(this));

    cmd.command("orchestration")
      .description("Demonstrate cross-service workflow")
      .option("-p, --product <id>", "Product ID")
      .action(this.orchestration.bind(this));

    cmd.command("embedding-orchestration")
      .description("Embeddings workflow with verification")
      .option("-t, --text <text>", "Text to process")
      .action(this.embeddingOrchestration.bind(this));

    cmd.command("local-models-test")
      .description("Test local-models service")
      .action(this.localModelsTest.bind(this));
  }

  async health() {
    const spinner = ora("Checking all service health...").start();
    try {
      const [aiHealth, tawheedHealth, localHealth] = await Promise.all([
        this.aiClient.health().catch(() => ({ status: "unavailable" })),
        this.tawheedClient.health().catch(() => ({ status: "unavailable" })),
        this.localModelsClient.health().catch(() => ({ status: "unavailable" })),
      ]);

      spinner.succeed("Health check complete");

      console.log(chalk.cyan("\n🏥 Service Health\n"));
      console.log(chalk.green(`  ✓ ai-inference:  ${aiHealth.status || "unknown"}`));
      console.log(chalk.green(`  ✓ tawheed:       ${tawheedHealth.status || "unknown"}`));
      console.log(chalk.green(`  ✓ local-models:  ${localHealth.status || "unknown"}`));

      console.log(chalk.cyan("\n📍 Service URLs:"));
      console.log(chalk.dim(`    ai-inference:  ${this.aiClient.getBaseUrl()}`));
      console.log(chalk.dim(`    tawheed:       ${this.tawheedClient.getBaseUrl()}`));
      console.log(chalk.dim(`    local-models:  ${this.localModelsClient.getBaseUrl()}`));

    } catch (e) {
      spinner.fail(e.message);
      process.exit(1);
    }
  }

  async orchestration(options) {
    const spinner = ora("Cross-service orchestration...").start();
    try {
      // 1. Generate embedding with ai-inference
      const text = options.product || "chicken biryani with basmati rice, halal certification required";
      const embedding = await this.aiClient.embeddings(text, "halalchain-local-v1");

      // 2. Evaluate with tawheed
      const evaluation = await this.tawheedClient.evaluatePolicy("prod-123", {
        embedding: embedding.embedding,
        source: "cross-service-orchestration",
        confidence_threshold: 0.7,
      });

      // 3. If needed, enhance with local-models
      let finalResult;
      if (evaluation.status === "manual_review" || evaluation.confidence < 0.7) {
        const enhanced = await this.localModelsClient.generate(
          `Evaluate halal compliance: ${text}\n` +
          `Previous evaluation: ${JSON.stringify(evaluation, null, 2)}\n` +
          `Provide detailed analysis with confidence score.`
        );
        finalResult = enhanced;
      } else {
        finalResult = evaluation;
      }

      spinner.succeed("Cross-service orchestration complete");

      console.log(chalk.cyan("\n🔗 Cross-Service Orchestration\n"));
      console.log(chalk.bold("Input:"), text);
      console.log(chalk.bold("Service Chain:"), "ai-inference → tawheed → local-models");
      console.log(chalk.bold("Final Result:"), JSON.stringify(finalResult, null, 2));

    } catch (e) {
      spinner.fail(e.message);
      process.exit(1);
    }
  }

  async embeddingOrchestration(options) {
    const spinner = ora("Embedding orchestration with verification...").start();
    try {
      const text = options.text || "organic chicken breast, farm-raised, halal certified";

      // Step 1: Generate embedding
      const embedding = await this.aiClient.embeddings(text, "production");

      // Step 2: Classify with tawheed (evidence)
      const classification = await this.tawheedClient.queryEvidence(
        `Is this product halal? Text: "${text}". Embedding similarity: ${embedding.embedding[0].toFixed(4)}.`,
        { top_k: 5 }
      );

      // Step 3: Enhance with local-models if available
      const localResponse = await this.localModelsClient.classify(text, ["halal", "haram", "unknown"], "classification-boost");

      spinner.succeed("Embedding orchestration complete");

      console.log(chalk.cyan("\n🔄 Embedding Orchestration\n"));
      console.log(chalk.bold("Original:"), text);
      console.log(chalk.bold("Embedding (ai-inference):"), embedding.embedding.map(v => v.toFixed(4)).join(", "));
      console.log(chalk.bold("Classification (tawheed):"), JSON.stringify(classification, null, 2));
      console.log(chalk.bold("Enhanced (local-models):"), JSON.stringify(localResponse, null, 2));

    } catch (e) {
      spinner.fail(e.message);
      process.exit(1);
    }
  }

  async localModelsTest() {
    const spinner = ora("Testing local-models service...").start();
    try {
      // Test embeddings
      const embedding = await this.localModelsClient.embeddings(
        "test embedding text for local-models service",
        "default"
      );

      // Test classification
      const classification = await this.localModelsClient.classify(
        "chicken biryani",
        ["halal", "haram"],
        "classification"
      );

      // Test generation
      const generation = await this.localModelsClient.generate(
        "Explain why this product is halal",
        { maxTokens: 100, temperature: 0.7, model: "default" }
      );

      // Test health
      const health = await this.localModelsClient.health();

      spinner.succeed("Local-models service test complete");

      console.log(chalk.cyan("\n🧪 Local-Models Service Test\n"));
      console.log(chalk.bold("Health Check:"), health.status);
      console.log(chalk.bold("Embedding:"), embedding.embedding.map(v => v.toFixed(4)).join(", "));
      console.log(chalk.bold("Classification:"), JSON.stringify(classification, null, 2));
      console.log(chalk.bold("Generation:"), generation.response);

    } catch (e) {
      spinner.fail(e.message);
      process.exit(1);
    }
  }
}