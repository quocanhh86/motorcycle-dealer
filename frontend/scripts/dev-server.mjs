import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { resolveVendorSourcePath, stageVendorAssets } from './stage-vendor.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const FRONTEND_ROOT = path.resolve(__dirname, '..');
const SRC_ROOT = path.join(FRONTEND_ROOT, 'src');

const MIME_TYPES = Object.freeze({
  '.html': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.mjs': 'application/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.webp': 'image/webp',
  '.avif': 'image/avif',
  '.ico': 'image/x-icon',
  '.ttf': 'font/ttf',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.map': 'application/json; charset=utf-8'
});

const SECURITY_HEADERS = Object.freeze({
  'X-Content-Type-Options': 'nosniff',
  'X-Frame-Options': 'SAMEORIGIN',
  'Referrer-Policy': 'strict-origin-when-cross-origin',
  'Cache-Control': 'no-cache'
});

function sendResponse(res, statusCode, contentType, bodyBuffer, method) {
  res.writeHead(statusCode, {
    ...SECURITY_HEADERS,
    'Content-Type': contentType,
    'Content-Length': Buffer.byteLength(bodyBuffer)
  });
  if (method === 'HEAD') {
    res.end();
    return;
  }
  res.end(bodyBuffer);
}

function tryDecodePathname(urlPathname) {
  if (typeof urlPathname !== 'string' || urlPathname.includes('\0')) {
    return null;
  }
  try {
    const decoded = decodeURIComponent(urlPathname);
    if (decoded.includes('\0')) {
      return null;
    }
    return decoded;
  } catch {
    return null;
  }
}

function resolveSafeStaticFile(urlPathname) {
  const decoded = tryDecodePathname(urlPathname);
  if (decoded === null) {
    return null;
  }
  const normalizedRel = decoded.replace(/^\/+/, '');
  const candidatePath = path.resolve(SRC_ROOT, normalizedRel);

  const relCheck = path.relative(SRC_ROOT, candidatePath);
  if (relCheck.startsWith('..') || path.isAbsolute(relCheck)) {
    return null;
  }

  if (fs.existsSync(candidatePath) && fs.statSync(candidatePath).isFile()) {
    return candidatePath;
  }

  if (normalizedRel.startsWith('vendor/')) {
    const vendorName = path.basename(normalizedRel);
    if (`vendor/${vendorName}` === normalizedRel.replace(/\\/g, '/')) {
      return resolveVendorSourcePath(vendorName);
    }
  }

  return null;
}

export function createSpaServer() {
  try {
    stageVendorAssets({ quiet: true });
  } catch {
    // Fallback resolution from node_modules remains active per-request.
  }

  return http.createServer((req, res) => {
    const method = (req.method || 'GET').toUpperCase();
    if (method !== 'GET' && method !== 'HEAD') {
      const body = JSON.stringify({ status: 405, title: 'Method Not Allowed' });
      sendResponse(res, 405, 'application/problem+json; charset=utf-8', Buffer.from(body, 'utf8'), method);
      return;
    }

    let pathname = '/';
    try {
      const parsedUrl = new URL(req.url || '/', 'http://localhost');
      pathname = parsedUrl.pathname || '/';
    } catch {
      const body = JSON.stringify({
        type: 'https://httpstatuses.io/400',
        status: 400,
        title: 'Malformed Request URI',
        detail: 'Request URI could not be parsed.'
      });
      sendResponse(res, 400, 'application/problem+json; charset=utf-8', Buffer.from(body, 'utf8'), method);
      return;
    }

    const decodedPathname = tryDecodePathname(pathname);
    if (decodedPathname === null) {
      const body = JSON.stringify({
        type: 'https://httpstatuses.io/400',
        status: 400,
        title: 'Malformed Request URI',
        detail: 'Request path contains invalid URI percent-encoding or a disallowed NUL byte.'
      });
      sendResponse(res, 400, 'application/problem+json; charset=utf-8', Buffer.from(body, 'utf8'), method);
      return;
    }

    if (decodedPathname === '/health') {
      const payload = JSON.stringify({
        status: 'Healthy',
        service: 'motorcycle-dealer-spa',
        timestampUtc: new Date().toISOString()
      });
      sendResponse(res, 200, 'application/json; charset=utf-8', Buffer.from(payload, 'utf8'), method);
      return;
    }

    if (decodedPathname === '/' || decodedPathname === '/index.html') {
      const indexPath = path.join(SRC_ROOT, 'index.html');
      const htmlBuf = fs.readFileSync(indexPath);
      sendResponse(res, 200, MIME_TYPES['.html'], htmlBuf, method);
      return;
    }

    const staticFile = resolveSafeStaticFile(pathname);
    if (staticFile) {
      const ext = path.extname(staticFile).toLowerCase();
      const contentType = MIME_TYPES[ext] || 'application/octet-stream';
      const fileBuf = fs.readFileSync(staticFile);
      sendResponse(res, 200, contentType, fileBuf, method);
      return;
    }

    const hasAssetExtension = path.extname(decodedPathname) !== '';
    if (hasAssetExtension) {
      const notFoundBody = JSON.stringify({
        type: 'https://httpstatuses.io/404',
        title: 'Static Asset Not Found',
        status: 404,
        detail: `Asset '${decodedPathname}' does not exist in frontend/src.`
      });
      sendResponse(res, 404, 'application/problem+json; charset=utf-8', Buffer.from(notFoundBody, 'utf8'), method);
      return;
    }

    // HTML5 pushState / hashbang SPA navigation fallback
    const fallbackIndex = path.join(SRC_ROOT, 'index.html');
    const fallbackBuf = fs.readFileSync(fallbackIndex);
    sendResponse(res, 200, MIME_TYPES['.html'], fallbackBuf, method);
  });
}

const isDirectRun = process.argv[1] && path.resolve(process.argv[1]) === __filename;
if (isDirectRun) {
  const port = Number.parseInt(process.env.PORT || '4200', 10);
  const host = process.env.HOST || '0.0.0.0';
  const server = createSpaServer();

  server.listen(port, host, () => {
    console.log(`[motorcycle-dealer-spa] AngularJS 1.8.3 SPA listening on http://localhost:${port} (bind ${host}:${port})`);
  });

  const shutdown = () => {
    server.close(() => process.exit(0));
  };
  process.on('SIGINT', shutdown);
  process.on('SIGTERM', shutdown);
}
