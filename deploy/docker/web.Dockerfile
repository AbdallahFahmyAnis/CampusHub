# syntax=docker/dockerfile:1
FROM node:22-alpine AS build
WORKDIR /web
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci
COPY src/frontend ./
RUN npx ng build shell --configuration production

FROM nginx:1.27-alpine
COPY deploy/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist/shell/browser /usr/share/nginx/html
EXPOSE 8080
