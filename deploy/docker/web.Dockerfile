# Masroof web (Angular) — multi-stage build
FROM node:22-alpine AS build
WORKDIR /app
COPY src/masroof-web/package*.json ./
RUN npm ci
COPY src/masroof-web/ .
RUN npm run build -- --configuration production

FROM nginxinc/nginx-unprivileged:alpine
COPY deploy/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/masroof-web/browser /usr/share/nginx/html
EXPOSE 8080
