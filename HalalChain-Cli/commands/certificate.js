import ora from "ora";
import chalk from "chalk";
import { APIClient } from "../lib/api-client.js";

export class CertificateCommand {
  constructor(program, configManager) {
    this.api = new APIClient(configManager);
    const cmd = program.command("certificate").description("Halal certificate extraction");
    cmd.command("extract")
      .description("Extract fields from halal certificate text")
      .option("-t, --text <text>", "Certificate text")
      .option("-f, --file <path>", "Read from file")
      .option("-j, --json", "Output as JSON")
      .action(this.extract.bind(this));
  }

  async extract(options) {
    let text = options.text;
    if (options.file) { const fs = await import("fs-extra"); text = await fs.readFile(options.file, "utf-8"); }
    if (!text) { const { input } = await import("@inquirer/prompts"); text = await input({ message: "Enter certificate text:", required: true }); }
    const spinner = ora("Extracting certificate fields...").start();
    try {
      const result = await this.api.certificateExtract(text);
      spinner.succeed("Certificate extracted");
      if (options.json) { console.log(JSON.stringify(result, null, 2)); return; }
      console.log(chalk.cyan("\n📜 Certificate Extraction\n"));
      console.log(chalk.bold("Completeness:"), result.completeness);
      console.log(chalk.bold("Fields:"));
      for (const [key, value] of Object.entries(result.parsed)) {
        console.log(`  ${chalk.bold(key)}: ${value || chalk.dim("(not found)")}`);
      }
    } catch (e) { spinner.fail(e.message); process.exit(1); }
  }
}