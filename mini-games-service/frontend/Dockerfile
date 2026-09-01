# Frontend container for mini-games-service.
#
# Unlike the other microservices this frontend is a Vue single-page app built
# with Vite. Each microservice is free to pick its own frontend stack; the only
# contract is that the container serves the app on port 80 and proxies /api/ to
# its own backend.
#
# The Node toolchain only exists in the build stage, so neither CI runners nor
# developer machines need Node installed to build this image.

FROM node:22-alpine AS build
WORKDIR /src

# Copy the manifests first so the dependency layer is cached independently.
COPY package*.json ./
RUN npm install

COPY index.html vite.config.js ./
COPY public/ ./public/
COPY src/ ./src/

# The shared-frontend gateway serves this app under /mini-games/, so asset
# URLs and API calls must be built with that base path.
ENV VITE_BASE=/mini-games/
RUN npm run build

FROM nginx:1.29-alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
    CMD wget --quiet --tries=1 --spider http://127.0.0.1/health || exit 1
