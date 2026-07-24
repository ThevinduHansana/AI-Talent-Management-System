# ---- Build stage ----
FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies from the lockfile (layer-cached).
COPY package.json package-lock.json ./
RUN npm ci

# Build the production bundle.
COPY . .
RUN npm run build

# ---- Serve stage ----
FROM nginx:1.27-alpine AS final

# SPA + reverse-proxy configuration.
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=5 \
    CMD wget -q --spider http://localhost:80/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
