FROM node:22-alpine AS build

WORKDIR /app

ARG TARGETOS=linux
ARG TARGETARCH

COPY package.json package-lock.json ./

# Keep optional native packages available for Tailwind's platform binding,
# but scope them to the target platform and make registry fetches more resilient.
RUN --mount=type=cache,target=/root/.npm \
    arch="$TARGETARCH" && \
    if [ "$arch" = "amd64" ]; then arch="x64"; fi && \
    npm ci \
      --include=optional \
      --os="$TARGETOS" \
      --cpu="$arch" \
      --libc=musl \
      --prefer-offline \
      --no-audit \
      --fetch-retries=5 \
      --fetch-retry-mintimeout=20000 \
      --fetch-retry-maxtimeout=120000

COPY . .

RUN npm run build

FROM node:22-alpine AS runtime

WORKDIR /app

ENV NODE_ENV=production

COPY package.json package-lock.json ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/dist ./dist

EXPOSE 4173

CMD ["npm", "run", "preview", "--", "--host", "0.0.0.0", "--port", "4173"]
