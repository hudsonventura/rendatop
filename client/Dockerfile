FROM node:22-alpine AS build

WORKDIR /app

COPY package.json package-lock.json ./

# Keep optional native packages available for Tailwind's platform binding.
RUN npm ci --include=optional

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
