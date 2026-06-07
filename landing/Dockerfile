# ---- Dependencies Stage ----
FROM node:22-alpine AS deps
WORKDIR /app/nextjs-version

ENV NEXT_TELEMETRY_DISABLED=1
ENV NPM_CONFIG_UPDATE_NOTIFIER=false
ENV NPM_CONFIG_FUND=false
ENV NPM_CONFIG_AUDIT=false

COPY nextjs-version/package.json nextjs-version/package-lock.json ./
RUN npm ci --no-audit --no-fund --fetch-retries=5 --fetch-retry-factor=2 --fetch-retry-mintimeout=20000 --fetch-retry-maxtimeout=120000

# ---- Build Stage ----
FROM node:22-alpine AS build
WORKDIR /app/nextjs-version

ARG NEXT_PUBLIC_BASE_URL_SERVER

ENV NEXT_TELEMETRY_DISABLED=1
ENV NPM_CONFIG_UPDATE_NOTIFIER=false
ENV NODE_OPTIONS=--max-old-space-size=2048
ENV NEXT_PUBLIC_BASE_URL_SERVER=$NEXT_PUBLIC_BASE_URL_SERVER
ENV NEXT_PUBLIC_BASE_URL_CLIENT=$BASE_URL_CLIENT

COPY --from=deps /app/nextjs-version/node_modules ./node_modules
COPY nextjs-version/ ./

RUN npx next build --webpack

# ---- Runtime Stage ----
FROM node:22-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production
ENV HOSTNAME=0.0.0.0
ENV PORT=3000
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

COPY --from=build /app/nextjs-version/public ./public
COPY --from=build --chown=nextjs:nodejs /app/nextjs-version/.next/standalone ./
COPY --from=build --chown=nextjs:nodejs /app/nextjs-version/.next/static ./.next/static

USER nextjs

EXPOSE 3000

CMD ["node", "server.js"]
