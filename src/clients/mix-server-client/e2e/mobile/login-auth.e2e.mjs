import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';
import puppeteer from 'puppeteer-core';

const devtoolsUrl = process.env.MIXSERVER_E2E_DEVTOOLS_URL ?? 'http://127.0.0.1:9222';
const serverUrl = process.env.MIXSERVER_E2E_SERVER_URL ?? 'http://10.0.2.2:5225';
const username = process.env.MIXSERVER_E2E_USERNAME ?? 'android-e2e';
const password = process.env.MIXSERVER_E2E_PASSWORD ?? 'AndroidE2E!123';
const loginPath = '/auth/user/login';
const successPaths = new Set([
  '/files',
  '/auth/user/reset-password',
]);
const screenshotPath = path.resolve(
  process.cwd(),
  '..',
  '..',
  '..',
  '..',
  '..',
  'data',
  'logs',
  'android-auth-result.png');

function toPathname(url) {
  return new URL(url).pathname;
}

async function getApplicationPage(browser) {
  const pages = await browser.pages();
  const page = pages.find(candidate => candidate.url().includes('localhost'));

  assert.ok(page, 'Could not find the Mix Server WebView page in the forwarded DevTools target.');

  return page;
}

async function ensureLoginPage(page) {
  const currentUrl = page.url();
  const origin = new URL(currentUrl).origin;
  const targetUrl = `${origin}${loginPath}`;

  if (currentUrl !== targetUrl) {
    await page.goto(targetUrl, { waitUntil: 'domcontentloaded' });
  }

  await page.waitForNetworkIdle();
}

async function seedServerUrl(page) {
  await page.evaluate(value => {
    localStorage.clear();
    localStorage.setItem('mixserver_server_url', value);
  }, serverUrl);

  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.waitForNetworkIdle();
}

async function performLogin(page) {
  await page.waitForSelector('input[type="text"]', { visible: true });
  await page.type('input[type="text"]', username);
  await page.type('input[type="password"]', password);
  await page.click('button.mat-mdc-button');
}

async function waitForAuthenticatedRoute(page) {
  await page.waitForFunction(expectedPaths => expectedPaths.includes(window.location.pathname), {
    timeout: 20000,
  }, Array.from(successPaths));
}

async function assertAuthenticatedState(page) {
  const currentUrl = page.url();
  const pathname = toPathname(currentUrl);

  assert.ok(
    successPaths.has(pathname),
    `Expected login to finish on an authenticated page, but landed on ${currentUrl}.`);

  const storage = await page.evaluate(() => ({
    accessToken: localStorage.getItem('accessToken'),
    deviceId: localStorage.getItem('deviceId'),
    refreshToken: localStorage.getItem('refreshToken'),
    serverUrl: localStorage.getItem('mixserver_server_url'),
  }));

  assert.ok(storage.accessToken, 'Login did not persist an access token.');
  assert.ok(storage.refreshToken, 'Login did not persist a refresh token.');
  assert.ok(storage.deviceId, 'Login did not persist a device id.');
  assert.equal(storage.serverUrl, serverUrl, 'The native server URL did not match the requested API URL.');

  return {
    pathname,
    storage,
  };
}

async function main() {
  await fs.mkdir(path.dirname(screenshotPath), { recursive: true });

  const browser = await puppeteer.connect({
    browserURL: devtoolsUrl,
    defaultViewport: null,
  });
  const page = await getApplicationPage(browser);
  const consoleMessages = [];

  page.on('console', message => {
    consoleMessages.push(`${message.type()}: ${message.text()}`);
  });

  try {
    await ensureLoginPage(page);
    await seedServerUrl(page);
    await performLogin(page);
    await waitForAuthenticatedRoute(page);

    const result = await assertAuthenticatedState(page);
    await page.screenshot({
      fullPage: true,
      path: screenshotPath,
    });

    console.log(JSON.stringify({
      result: 'passed',
      route: result.pathname,
      screenshotPath,
      serverUrl,
      username,
    }, null, 2));
  } catch (error) {
    await page.screenshot({
      fullPage: true,
      path: screenshotPath,
    }).catch(() => undefined);

    const details = error instanceof Error
      ? {
          message: error.message,
          stack: error.stack,
        }
      : {
          message: String(error),
        };

    console.error(JSON.stringify({
      result: 'failed',
      consoleMessages,
      screenshotPath,
      serverUrl,
      username,
      ...details,
    }, null, 2));

    throw error;
  } finally {
    await browser.close();
  }
}

await main();
