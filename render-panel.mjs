import puppeteer from 'puppeteer';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const args = process.argv.slice(2);
const full = args.includes('--full');
const id = args.find(a => !a.startsWith('--'));
if (!id) {
    console.error('Usage: node render-panel.mjs <item-id> [--full]');
    console.error('Example: node render-panel.mjs 0x7902F2C7');
    console.error('         node render-panel.mjs 0x7902F2C7 --full');
    process.exit(1);
}

const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.setViewport({ width: 1400, height: 1200 });
await page.goto(`http://localhost:5138/Item/id/${id}`, { waitUntil: 'networkidle0' });

const __dirname = dirname(fileURLToPath(import.meta.url));
const outPath = join(__dirname, 'temp', `render-${id}.png`);

if (full) {
    await page.screenshot({ path: outPath, fullPage: true });
} else {
    const panel = await page.$('.mock-side');
    await panel.screenshot({ path: outPath });
}

await browser.close();
console.log(`Saved to ${outPath}`);
