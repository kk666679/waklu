import { readFileSync } from "node:fs";

export function readFileContent(path) {
  return readFileSync(path, "utf-8");
}

export function parseFileContent(content, ext) {
  if (ext === "json" || ext === "json5") return JSON.parse(content);
  if (ext === "yaml" || ext === "yml") {
    // Simple YAML-ish parse for flat key: value
    const lines = content.split("\n").filter(l => l.trim() && !l.startsWith("#"));
    const obj = {};
    for (const line of lines) {
      const m = line.match(/^([^:]+):\s*(.*)$/);
      if (m) obj[m[1].trim()] = m[2].trim();
    }
    return obj;
  }
  if (ext === "csv") return content.split("\n").filter(l => l.trim());
  return content;
}

export function cosineSimilarity(a, b) {
  if (!a || !b || a.length !== b.length) return 0;
  let dot = 0, magA = 0, magB = 0;
  for (let i = 0; i < a.length; i++) {
    dot += a[i] * b[i];
    magA += a[i] * a[i];
    magB += b[i] * b[i];
  }
  return dot / (Math.sqrt(magA) * Math.sqrt(magB) || 1);
}

export function truncate(str, maxLen = 60) {
  if (!str) return "";
  return str.length > maxLen ? str.slice(0, maxLen) + "..." : str;
}

export function formatBytes(bytes) {
  if (bytes < 1024) return bytes + " B";
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + " KB";
  return (bytes / (1024 * 1024)).toFixed(1) + " MB";
}
