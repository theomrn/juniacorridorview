FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# VITE_API_BASE_URL vide = URL relative, nginx proxy /api/* vers l'API
ARG VITE_API_BASE_URL=""
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npx vite build --config vite.config.js

FROM nginx:alpine AS runtime
COPY --from=build /app/src/client/dist /usr/share/nginx/html
COPY docker/client/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
