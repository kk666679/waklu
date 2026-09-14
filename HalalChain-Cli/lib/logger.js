import chalk from "chalk";

let level = "info";
const levels = { debug: 0, info: 1, warn: 2, error: 3 };

export const Logger = {
  setLevel(l) { level = l; },
  debug: (...args) => levels[level] <= 0 && console.log(chalk.gray("[debug]", ...args)),
  info: (...args) => levels[level] <= 1 && console.log(...args),
  warn: (...args) => levels[level] <= 2 && console.warn(chalk.yellow("[warn]", ...args)),
  error: (...args) => console.error(chalk.red("[error]", ...args)),
  success: (msg) => console.log(chalk.green("✔"), msg),
  fail: (msg) => console.error(chalk.red("✖"), msg),
};
