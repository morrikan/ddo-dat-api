import puppeteer from 'puppeteer';

const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.setViewport({ width: 1400, height: 1200 });
await page.goto('http://localhost:5138/Item/id/2030156231', { waitUntil: 'networkidle0' });

// Screenshot just the mock-side panel
const panel = await page.$('.mock-side');
await panel.screenshot({ path: 'C:\\dev\\temp\\render-panel.png' });

await browser.close();
console.log('Saved to /c/dev/temp/render-panel.png');
