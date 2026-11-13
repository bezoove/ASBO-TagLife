#!/usr/bin/env node
// Usage: npm run sync-game -- /path/to/eggy-duck.html   (or GAME_HTML=/path npm run sync-game)
// Copies the game to www/index.html and (idempotently) appends the tiny HUD loader.
const fs = require('fs');
const path = require('path');
const src = process.argv[2] || process.env.GAME_HTML;
if (!src || !fs.existsSync(src)) {
  console.error('Usage: npm run sync-game -- /path/to/game.html  (or set GAME_HTML)');
  process.exit(1);
}
const root = path.join(__dirname, '..');
const dest = path.join(root, 'www', 'index.html');
let html = fs.readFileSync(src, 'utf8');
html = html.replace(/<!--asbo-hud-loader-->[\s\S]*?<!--\/asbo-hud-loader-->\s*/g, '');
const snippet = fs.readFileSync(path.join(__dirname, 'hud-loader.snippet.html'), 'utf8').trim();
const m = html.match(/<\/body>/i);
html = m ? html.replace(/<\/body>(?![\s\S]*<\/body>)/i, snippet + '\n</body>') : html.trimEnd() + '\n' + snippet + '\n';
fs.writeFileSync(dest, html);
console.log(`Wrote ${dest} (${(fs.statSync(dest).size / 1048576).toFixed(1)} MB) with HUD loader.`);
