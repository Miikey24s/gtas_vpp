const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({
    headless: true,
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe'
  });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1 });
  await page.goto('http://127.0.0.1:5203/Account/Login', { waitUntil: 'networkidle', timeout: 60000 });
  await page.evaluate(() => document.fonts.ready);
  await page.screenshot({ path: 'D:/WORK/gtas_vpp/tmp/03d-login-current.png', fullPage: true });
  console.log(JSON.stringify({ url: page.url(), title: await page.title() }));
  await browser.close();
})().catch(error => {
  console.error(error);
  process.exit(1);
});
