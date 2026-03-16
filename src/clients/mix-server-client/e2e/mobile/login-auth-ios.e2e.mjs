/**
 * iOS authentication end-to-end test.
 *
 * Connects to the Capacitor WKWebView running inside the iPhone simulator via
 * ios-webkit-debug-proxy, which exposes the WebKit Remote Debug Protocol over
 * HTTP/WebSocket on localhost.  The thin WebKitPage wrapper below implements
 * just enough of the Puppeteer-like API used in login-auth.e2e.mjs to drive
 * the login flow without any external browser-automation dependency.
 */

import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import fs from 'node:fs/promises';
import http from 'node:http';
import path from 'node:path';
import process from 'node:process';
import { WebSocket } from 'ws';

const devtoolsPort   = parseInt(process.env.MIXSERVER_E2E_DEVTOOLS_PORT ?? '9222', 10);
const serverUrl      = process.env.MIXSERVER_E2E_SERVER_URL    ?? 'http://localhost:5225';
const username       = process.env.MIXSERVER_E2E_USERNAME      ?? 'ios-e2e';
const password       = process.env.MIXSERVER_E2E_PASSWORD      ?? 'iOSE2E!123';
const simulatorUdid  = process.env.MIXSERVER_E2E_SIMULATOR_UDID ?? 'booted';

const loginPath    = '/auth/user/login';
const successPaths = new Set(['/files', '/auth/user/reset-password']);

const screenshotPath = path.resolve(
  process.cwd(),
  '..', '..', '..', '..', '..',
  'data', 'logs', 'ios-auth-result.png',
);

// ─── Utilities ────────────────────────────────────────────────────────────────

function sleep(ms) {
  return new Promise(r => setTimeout(r, ms));
}

function httpGetJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, res => {
      let raw = '';
      res.on('data', chunk => (raw += chunk));
      res.on('end', () => {
        try { resolve(JSON.parse(raw)); }
        catch (e) { reject(new Error(`JSON parse error from ${url}: ${e.message}`)); }
      });
    }).on('error', reject);
  });
}

// ─── WebKit Remote Debug Protocol client ─────────────────────────────────────

class WebKitPage {
  constructor(ws) {
    this._ws  = ws;
    this._seq = 1;
    this._pending = new Map();

    ws.on('message', raw => {
      let msg;
      try { msg = JSON.parse(raw.toString()); } catch { return; }

      if (msg.id !== undefined) {
        const cb = this._pending.get(msg.id);
        if (cb) {
          this._pending.delete(msg.id);
          if (msg.error) cb.reject(new Error(msg.error.message ?? JSON.stringify(msg.error)));
          else           cb.resolve(msg.result ?? {});
        }
      }
    });
  }

  /**
   * Enable required inspector domains (must be called once after connecting).
   * Inspector.enable activates the WebKit inspector session; other domains
   * become available only after this handshake completes.
   */
  async enable() {
    await this._send('Inspector.enable').catch(() => {});
    await sleep(500);
    await this._send('Runtime.enable').catch(() => {});
    await this._send('Page.enable').catch(() => {});
    await this._send('Network.enable').catch(() => {});
  }

  /** Send a WebKit Inspector Protocol command and await its response. */
  _send(method, params = {}) {
    return new Promise((resolve, reject) => {
      const id = this._seq++;
      this._pending.set(id, { resolve, reject });
      this._ws.send(JSON.stringify({ id, method, params }));
    });
  }

  /** Evaluate a JavaScript expression in the page context. */
  async evaluate(expression) {
    const { result } = await this._send('Runtime.evaluate', {
      expression,
      returnByValue: true,
    });
    if (result?.subtype === 'error') throw new Error(result.description);
    if (result?.type === 'undefined') return undefined;
    return result?.value;
  }

  /** Return the current page URL. */
  url() {
    return this.evaluate('window.location.href');
  }

  /** Navigate to a URL and wait for the document to be interactive. */
  async goto(targetUrl) {
    await this._send('Page.navigate', { url: targetUrl });
    await this._waitForReady();
  }

  /** Reload the current page and wait for the document to be interactive. */
  async reload() {
    await this._send('Page.reload', {});
    await this._waitForReady();
  }

  /** Poll document.readyState until complete. */
  async _waitForReady() {
    for (let i = 0; i < 30; i++) {
      await sleep(500);
      try {
        const state = await this.evaluate('document.readyState');
        if (state === 'complete') return;
      } catch { /* page may still be navigating */ }
    }
  }

  /** Simple timeout-based network-idle substitute. */
  async waitForNetworkIdle() {
    await sleep(2500);
  }

  /** Poll until a CSS selector matches an element in the DOM. */
  async waitForSelector(selector, options = {}) {
    const deadline = Date.now() + (options.timeout ?? 30_000);
    while (Date.now() < deadline) {
      const found = await this.evaluate(
        `!!document.querySelector(${JSON.stringify(selector)})`,
      ).catch(() => false);
      if (found) return;
      await sleep(500);
    }
    throw new Error(`Timeout waiting for selector: ${selector}`);
  }

  /**
   * Set the value of an <input> using React/Angular-compatible native setter,
   * then dispatch input + change events so the framework picks up the change.
   */
  async type(selector, text) {
    const js = `(function () {
      var el = document.querySelector(${JSON.stringify(selector)});
      if (!el) throw new Error('Element not found: ' + ${JSON.stringify(selector)});
      el.focus();
      var proto = el instanceof HTMLTextAreaElement
        ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
      var setter = Object.getOwnPropertyDescriptor(proto, 'value')?.set;
      if (setter) setter.call(el, ${JSON.stringify(text)});
      else el.value = ${JSON.stringify(text)};
      el.dispatchEvent(new Event('input',  { bubbles: true }));
      el.dispatchEvent(new Event('change', { bubbles: true }));
    })()`;
    await this.evaluate(js);
  }

  /** Click an element matched by a CSS selector. */
  async click(selector) {
    const js = `(function () {
      var el = document.querySelector(${JSON.stringify(selector)});
      if (!el) throw new Error('Element not found: ' + ${JSON.stringify(selector)});
      el.click();
    })()`;
    await this.evaluate(js);
  }

  /**
   * Poll until the supplied predicate (called with extra args inside the page)
   * returns a truthy value.
   */
  async waitForFunction(fn, options = {}, ...args) {
    const deadline  = Date.now() + (options.timeout ?? 30_000);
    const fnStr     = fn.toString();
    const argsJson  = args.map(a => JSON.stringify(a)).join(', ');
    while (Date.now() < deadline) {
      const result = await this.evaluate(`(${fnStr})(${argsJson})`).catch(() => false);
      if (result) return;
      await sleep(500);
    }
    throw new Error('Timeout in waitForFunction');
  }

  /** Take a screenshot of the booted simulator screen via simctl. */
  screenshot(opts = {}) {
    if (!opts.path) return;
    execFileSync('xcrun', ['simctl', 'io', simulatorUdid, 'screenshot', opts.path]);
  }

  /** No-op — console message capture is not implemented in this thin client. */
  on() {}

  close() { this._ws.close(); }
}

// ─── Proxy / page discovery ───────────────────────────────────────────────────

/** Poll ios-webkit-debug-proxy until the Mix Server WebView page appears. */
async function waitForAppPage(port) {
  for (let attempt = 0; attempt < 30; attempt++) {
    try {
      const pages = await httpGetJson(`http://127.0.0.1:${port}/json`);
      const page  = pages.find(p => p.url?.includes('localhost') && p.webSocketDebuggerUrl);
      if (page) return page;
    } catch { /* proxy not ready yet */ }
    await sleep(2000);
  }
  throw new Error(
    `No Mix Server WebView page found at http://127.0.0.1:${port}/json after 60 seconds.\n` +
    `Ensure the app launched on the simulator and ios-webkit-debug-proxy is connected.`,
  );
}

/** Open a WebSocket connection to the given URL. */
function connectWs(wsUrl) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(wsUrl);
    ws.once('open',  () => resolve(ws));
    ws.once('error', reject);
    setTimeout(() => reject(new Error(`WebSocket timed out: ${wsUrl}`)), 10_000);
  });
}

// ─── Test logic (mirrors login-auth.e2e.mjs) ─────────────────────────────────

function toPathname(url) {
  return new URL(url).pathname;
}

async function ensureLoginPage(page) {
  const currentUrl = await page.url();
  const origin     = new URL(currentUrl).origin;
  const targetUrl  = `${origin}${loginPath}`;

  if (currentUrl !== targetUrl) {
    await page.goto(targetUrl);
  }

  await page.waitForNetworkIdle();
}

async function seedServerUrl(page) {
  await page.evaluate(`
    localStorage.clear();
    localStorage.setItem('mixserver_server_url', ${JSON.stringify(serverUrl)});
  `);
  await page.reload();
  await page.waitForNetworkIdle();
}

async function performLogin(page) {
  await page.waitForSelector('input[type="text"]', { visible: true });
  await page.type('input[type="text"]',      username);
  await page.type('input[type="password"]',  password);
  await page.click('button.mat-mdc-button');
}

async function waitForAuthenticatedRoute(page) {
  await page.waitForFunction(
    expectedPaths => expectedPaths.includes(window.location.pathname),
    { timeout: 20_000 },
    Array.from(successPaths),
  );
}

async function assertAuthenticatedState(page) {
  const currentUrl = await page.url();
  const pathname   = toPathname(currentUrl);

  assert.ok(
    successPaths.has(pathname),
    `Expected login to finish on an authenticated page, but landed on ${currentUrl}.`,
  );

  const storage = await page
    .evaluate(`JSON.stringify({
      accessToken:  localStorage.getItem('accessToken'),
      deviceId:     localStorage.getItem('deviceId'),
      refreshToken: localStorage.getItem('refreshToken'),
      serverUrl:    localStorage.getItem('mixserver_server_url'),
    })`)
    .then(JSON.parse);

  assert.ok(storage.accessToken,  'Login did not persist an access token.');
  assert.ok(storage.refreshToken, 'Login did not persist a refresh token.');
  assert.ok(storage.deviceId,     'Login did not persist a device id.');
  assert.equal(storage.serverUrl, serverUrl, 'The native server URL did not match the requested API URL.');

  return { pathname, storage };
}

// ─── Entry point ─────────────────────────────────────────────────────────────

async function main() {
  await fs.mkdir(path.dirname(screenshotPath), { recursive: true });

  const pageInfo = await waitForAppPage(devtoolsPort);

  let ws;
  try {
    ws = await connectWs(pageInfo.webSocketDebuggerUrl);
  } catch (err) {
    throw new Error(
      `Failed to open WebSocket to ${pageInfo.webSocketDebuggerUrl}: ${err.message}`,
    );
  }

  const page = new WebKitPage(ws);
  await page.enable();

  try {
    await ensureLoginPage(page);
    await seedServerUrl(page);
    await performLogin(page);
    await waitForAuthenticatedRoute(page);

    const result = await assertAuthenticatedState(page);

    page.screenshot({ path: screenshotPath });

    console.log(JSON.stringify({
      result: 'passed',
      route:  result.pathname,
      screenshotPath,
      serverUrl,
      username,
    }, null, 2));
  } catch (error) {
    try { page.screenshot({ path: screenshotPath }); } catch { /* best-effort */ }

    const details = error instanceof Error
      ? { message: error.message, stack: error.stack }
      : { message: String(error) };

    console.error(JSON.stringify({
      result: 'failed',
      screenshotPath,
      serverUrl,
      username,
      ...details,
    }, null, 2));

    throw error;
  } finally {
    page.close();
  }
}

await main();
