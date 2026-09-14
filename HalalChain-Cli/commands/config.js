import { Command } from "commander";
import { input, select, confirm } from "@inquirer/prompts";
import chalk from "chalk";
import ora from "ora";
import { get, set, getAll, reset, configPath, KNOWN_KEYS } from "../lib/store.js";
import { generateSecret, validateJwtKey } from "../lib/secrets.js";

export function configCommand() {
  const cmd = new Command("config").description("Manage local HalalChain config (~/.halalchain/config.json)");

  cmd.command("init").description("Interactive setup wizard").action(async () => {
    console.log(chalk.bold("\n🌙 HalalChain config init\n"));
    const platformUrl = await input({ message: "Platform API URL", default: get("platform-api.url") });
    const marketplaceUrl = await input({ message: "Marketplace URL", default: get("marketplace.url") });
    const aiUrl = await input({ message: "AI Inference URL", default: get("ai-inference.url") });
    const tawheedUrl = await input({ message: "Tawheed URL", default: get("tawheed.url") });
    const jurisdiction = await select({
      message: "Default jurisdiction",
      choices: [
        { value: "MY", name: "MY — Malaysia (JAKIM / MS1500:2019)" },
        { value: "ID", name: "ID — Indonesia (MUI / BPJPH)" },
        { value: "SG", name: "SG — Singapore" },
        { value: "BN", name: "BN — Brunei" },
        { value: "GCC", name: "GCC — Gulf" },
        { value: "EU", name: "EU — Europe" },
      ],
      default: get("tawheed.jurisdiction") ?? "MY",
    });
    const generateKey = await confirm({ message: "Generate a new JWT secret key?", default: !get("jwt.key") });
    const jwtKey = generateKey
      ? generateSecret(32)
      : await input({ message: "JWT secret key (≥ 32 chars)", default: get("jwt.key") ?? "", validate: (v) => validateJwtKey(v) ?? true });

    const spinner = ora("Saving config…").start();
    set("platform-api.url", platformUrl);
    set("marketplace.url", marketplaceUrl);
    set("ai-inference.url", aiUrl);
    set("tawheed.url", tawheedUrl);
    set("tawheed.jurisdiction", jurisdiction);
    set("tawheed.policy-version", jurisdiction === "MY" ? "MY-v3" : `${jurisdiction}-v1`);
    set("jwt.key", jwtKey);
    spinner.succeed(chalk.green("Config saved → " + configPath()));
    if (generateKey) console.log(chalk.yellow("\n  JWT key:\n  ") + chalk.cyan(jwtKey + "\n"));
  });

  cmd.command("get <key>").description("Get a config value").action((key) => {
    const value = get(key);
    if (value === undefined) { console.error(chalk.red(`Unknown key: ${key}`)); process.exit(1); }
    console.log(value);
  });

  cmd.command("set <key> <value>").description("Set a config value").action((key, value) => {
    if (!KNOWN_KEYS.includes(key)) {
      console.error(chalk.red(`Unknown key: ${key}`));
      console.error(chalk.dim("Run `halalchain config list` to see valid keys."));
      process.exit(1);
    }
    if (key === "jwt.key") { const err = validateJwtKey(value); if (err) { console.error(chalk.red(err)); process.exit(1); } }
    set(key, value);
    const isSecret = key === "jwt.key" || key === "ai-inference.api-key";
    console.log(chalk.green(`✔ ${key} = ${isSecret && value ? "***" : value}`));
  });

  cmd.command("list").alias("ls").description("List all config values").action(() => {
    const all = getAll();
    const maxLen = Math.max(...Object.keys(all).map((k) => k.length));
    console.log(chalk.bold("\n  HalalChain config  ") + chalk.dim(configPath()) + "\n");
    for (const [key, value] of Object.entries(all)) {
      const isSecret = key === "jwt.key" || key === "ai-inference.api-key";
      const display = isSecret && value ? "***" : (value ?? chalk.dim("(unset)"));
      console.log("  " + chalk.cyan(key.padEnd(maxLen + 2)) + display);
    }
    console.log();
  });

  cmd.command("validate").description("Validate config for common issues").action(() => {
    const all = getAll();
    const errors = [];
    const jwtErr = validateJwtKey(all["jwt.key"]);
    if (jwtErr) errors.push(`jwt.key: ${jwtErr}`);
    for (const k of ["platform-api.url", "marketplace.url", "ai-inference.url", "tawheed.url"]) {
      try { new URL(all[k]); } catch { errors.push(`${k}: invalid URL "${all[k]}"`); }
    }
    const validJ = ["MY", "ID", "SG", "BN", "GCC", "EU"];
    if (!validJ.includes(all["tawheed.jurisdiction"]))
      errors.push(`tawheed.jurisdiction: must be one of ${validJ.join(", ")}`);
    if (errors.length) {
      console.error(chalk.red(`\n  ✖ ${errors.length} issue(s):\n`));
      errors.forEach((e) => console.error(chalk.red("    • " + e)));
      console.error();
      process.exit(1);
    }
    console.log(chalk.green("\n  ✔ Config is valid\n"));
  });

  cmd.command("reset").description("Reset config to defaults").action(async () => {
    const ok = await confirm({ message: "Reset all config to defaults?", default: false });
    if (!ok) { console.log("Aborted."); return; }
    reset();
    console.log(chalk.green("✔ Config reset to defaults."));
  });

  return cmd;
}
