import puppeteer from 'puppeteer';

const id = process.argv[2];
if (!id) {
    console.error('Usage: node render-panel.mjs <item-id>');
    console.error('Example: node render-panel.mjs 0x7902F2C7');
    process.exit(1);
}

const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.setViewport({ width: 1400, height: 1200 });
await page.goto(`http://localhost:5138/Item/id/${id}`, { waitUntil: 'networkidle0' });

// Screenshot just the mock-side panel
const panel = await page.$('.mock-side');
const outPath = new URL(`./temp/render-${id}.png`, import.meta.url).pathname;
await panel.screenshot({ path: outPath });

await browser.close();
console.log(`Saved to ${outPath}`);
