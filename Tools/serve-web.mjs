// Local-only preview of the Unity WebGL build. Run: node Tools/serve-web.mjs
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../Builds/WebGL');
const port = Number(process.env.SURVIVAL_PREVIEW_PORT || 8765);
const types = { '.html': 'text/html; charset=utf-8', '.js': 'application/javascript', '.wasm': 'application/wasm', '.data': 'application/octet-stream', '.json': 'application/json', '.png': 'image/png', '.css': 'text/css', '.ico': 'image/x-icon' };
http.createServer((request, response) => {
  let name;
  try { name = decodeURIComponent(new URL(request.url, 'http://127.0.0.1').pathname); }
  catch { response.writeHead(400).end(); return; }
  const target = path.resolve(root, '.' + (name === '/' ? '/index.html' : name));
  if (target !== root && !target.startsWith(root + path.sep)) { response.writeHead(403).end(); return; }
  fs.stat(target, (error, stat) => {
    if (error || !stat.isFile()) { response.writeHead(404).end('Build file not found. Build WebGL in Unity first.'); return; }
    response.writeHead(200, { 'Content-Type': types[path.extname(target)] || 'application/octet-stream', 'Content-Length': stat.size, 'Cache-Control': 'no-cache' });
    fs.createReadStream(target).pipe(response);
  });
}).listen(port, '127.0.0.1', () => console.log(`Survival Legend preview: http://127.0.0.1:${port}`));
