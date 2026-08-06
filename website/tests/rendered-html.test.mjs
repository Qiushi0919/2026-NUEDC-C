import assert from "node:assert/strict";
import { access, readFile } from "node:fs/promises";
import test from "node:test";

async function render() {
  const workerUrl = new URL("../dist/server/index.js", import.meta.url);
  workerUrl.searchParams.set("test", `${process.pid}-${Date.now()}`);
  const { default: worker } = await import(workerUrl.href);

  return worker.fetch(
    new Request("http://localhost/", { headers: { accept: "text/html" } }),
    { ASSETS: { fetch: async () => new Response("Not found", { status: 404 }) } },
    { waitUntil() {}, passThroughOnException() {} },
  );
}

test("server-renders the complete project page", async () => {
  const response = await render();
  assert.equal(response.status, 200);
  assert.match(response.headers.get("content-type") ?? "", /^text\/html\b/i);

  const html = await response.text();
  assert.match(html, /数字钥匙实验系统/);
  assert.match(html, /功能介绍视频/);
  assert.match(html, /定位原理与算法/);
  assert.match(html, /电路与程序设计/);
  assert.match(html, /测试结果/);
  assert.doesNotMatch(html, /现场演示 · H\.264/);
  assert.doesNotMatch(html, /从定位到门锁动作的完整闭环/);
});

test("keeps the design report and report figures in the published bundle", async () => {
  const page = await readFile(new URL("../app/page.tsx", import.meta.url), "utf8");
  assert.match(page, /2026-NUEDC-C-设计报告\.pdf/);
  assert.match(page, /report\/pdoa-principle\.png/);
  assert.match(page, /report\/uwb-transmitter-circuit\.png/);
  assert.match(page, /report\/program-flow\.png/);
  assert.doesNotMatch(page, /要求 1/);

  await Promise.all([
    access(new URL("../public/downloads/2026-NUEDC-C-设计报告.pdf", import.meta.url)),
    access(new URL("../public/report/test-results.png", import.meta.url)),
  ]);
});
