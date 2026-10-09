const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..', 'docs');
const port = Number(process.argv[2] || 8080);
const types = { '.html': 'text/html; charset=utf-8', '.js': 'application/javascript', '.wasm': 'application/wasm', '.css': 'text/css', '.png': 'image/png' };
http.createServer((request, response) => {
    let file;
    try {
        const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
        file = path.resolve(root, '.' + (pathname === '/' ? '/index.html' : pathname));
        if (!file.startsWith(root + path.sep)) throw new Error('Outside build directory');
    } catch {
        response.writeHead(400).end('Invalid path');
        return;
    }
    fs.stat(file, (error, stat) => {
        if (error || !stat.isFile()) { response.writeHead(404).end('Not found'); return; }
        response.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Content-Length': stat.size, 'Cache-Control': 'no-cache' });
        if (request.method === 'HEAD') response.end();
        else fs.createReadStream(file).pipe(response);
    });
}).listen(port, '127.0.0.1', () => console.log(`Fight against Israel: http://localhost:${port}`));
