FROM node:22-alpine AS build

WORKDIR /app

COPY client/package.json client/package-lock.json ./
COPY .env ./.env

# Keep optional native packages available for Tailwind's platform binding.
RUN npm ci --include=optional

COPY client/ .

RUN npm run build

FROM node:22-alpine AS runtime

WORKDIR /app

ENV NODE_ENV=production

COPY client/package.json client/package-lock.json ./
COPY client/vite.config.js ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/dist ./dist

EXPOSE 4173

CMD ["npm", "run", "preview", "--", "--host", "0.0.0.0", "--port", "4173"]
