// Tests for the CLI command modules that previously had runtime errors:
// inquirer prompts, Logger usage, and the monitor command that hit a
// non-existent POST /health.

import { test } from "node:test";
import assert from "node:assert/strict";
import { Command } from "commander";

const { MonitorCommand } = await import("../commands/monitor.js");
const { AIContextCommand } = await import("../commands/ai-context.js");
const { MLCommand } = await import("../commands/ml.js");

function makeFakeConfigManager() {
  return {
    get: (k, d) => d,
    set: () => {},
    delete: () => {},
    getAll: () => ({}),
  };
}

test("monitor: uses GET /health/ready (not POST /health)", async () => {
  const program = new Command();
  const seen = [];
  const fakeApi = {
    request: async (method, path) => { seen.push([method, path]); return { status: "ok", service: "ai" }; },
  };
  // We don't go through the constructor's APIClient wiring; we just
  // verify the request method/path combo by intercepting the api
  // instance. Easiest: monkey-patch APIClient on the prototype.
  const { APIClient } = await import("../lib/api-client.js");
  const orig = APIClient.prototype.request;
  APIClient.prototype.request = fakeApi.request;
  try {
    const cmd = new MonitorCommand(program, makeFakeConfigManager());
    await cmd.run();
  } finally {
    APIClient.prototype.request = orig;
  }
  // The first call should be a GET against /health/ready.
  assert.equal(seen[0][0], "GET");
  assert.match(seen[0][1], /\/health\/(ready|live)$/);
});

test("ai-context: options accept both --name and --context", () => {
  const program = new Command();
  // We just need to construct the command without it throwing.
  new AIContextCommand(program, makeFakeConfigManager());
  // Commander should have registered 'ai-context create' with both options.
  const aiCmd = program.commands.find((c) => c.name() === "ai-context");
  assert.ok(aiCmd, "ai-context subcommand must be registered");
  const createCmd = aiCmd.commands.find((c) => c.name() === "create");
  assert.ok(createCmd, "create subcommand must be registered");
  const opts = createCmd.options.map((o) => o.long);
  assert.ok(opts.includes("--name"));
  assert.ok(opts.includes("--context"));
});

test("ml: train and predict subcommands are registered", () => {
  const program = new Command();
  new MLCommand(program, makeFakeConfigManager());
  const ml = program.commands.find((c) => c.name() === "ml");
  assert.ok(ml);
  const subcommands = ml.commands.map((c) => c.name());
  for (const name of ["train", "predict", "models"]) {
    assert.ok(subcommands.includes(name), `ml ${name} must be registered`);
  }
});
