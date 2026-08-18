# syntax=docker/dockerfile:1
FROM node:22-alpine AS build
WORKDIR /app
COPY src/services/chat/package.json src/services/chat/package-lock.json ./
RUN npm ci --omit=dev
COPY src/services/chat/src ./src

FROM node:22-alpine
WORKDIR /app
RUN mkdir -p /data
COPY --from=build /app .
ENV PORT=8080
ENV DATA_DIR=/data
EXPOSE 8080
CMD ["node", "src/index.js"]
