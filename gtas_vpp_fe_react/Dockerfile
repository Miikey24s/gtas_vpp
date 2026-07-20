# GTAS VPP React — production build and unprivileged static runtime.

FROM node:24.17.0-alpine3.24 AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN --mount=type=cache,target=/root/.npm npm ci

COPY . .

# Production always uses same-origin HttpOnly cookie authentication. Keep
# build-time values scoped to this command so Docker does not treat them as
# runtime environment configuration or report a false secret warning.
RUN VITE_AUTH_MODE=cookie VITE_API_BASE_URL= npm run build

FROM nginxinc/nginx-unprivileged:1.30-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=5s \
  CMD wget -q -O /dev/null http://127.0.0.1:8080/healthz || exit 1
