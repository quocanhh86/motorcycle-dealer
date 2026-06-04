# syntax=docker/dockerfile:1
# ==============================================================================
# Motorcycle Dealer & Loyalty Management Platform — Frontend Dockerfile (T-308)
# Multi-stage Node 22 Alpine non-root AngularJS 1.8.3 SPA container (port 4200)
# ==============================================================================

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
COPY scripts/ ./scripts/

# Install production runtime dependencies cleanly without executing postinstall before src/ is copied
RUN if [ -f package-lock.json ]; then \
      npm ci --omit=dev --ignore-scripts; \
    else \
      npm install --omit=dev --ignore-scripts; \
    fi

COPY src/ ./src/

# Strip any stale host src/vendor directory before staging clean browser UMD bundles
RUN rm -rf src/vendor \
    && if [ -f scripts/stage-vendor.mjs ]; then node scripts/stage-vendor.mjs; fi

FROM node:22-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production \
    HOST=0.0.0.0 \
    PORT=4200

COPY --from=build --chown=node:node /app/package.json ./package.json
COPY --from=build --chown=node:node /app/node_modules ./node_modules
COPY --from=build --chown=node:node /app/scripts ./scripts
COPY --from=build --chown=node:node /app/src ./src

# Drop privileges to built-in non-root Alpine user (node: UID 1000)
USER node
EXPOSE 4200

HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
    CMD ["node", "-e", "fetch('http://127.0.0.1:4200/').then(r => process.exit(r.ok ? 0 : 1)).catch(() => process.exit(1))"]

CMD ["node", "scripts/dev-server.mjs"]
