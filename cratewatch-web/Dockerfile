FROM node:22-alpine AS build
WORKDIR /app
# package-lock.json is deliberately not copied: one generated on macOS lacks the Linux rollup binary (npm optional-deps bug).
COPY package.json ./
RUN npm install
COPY . .
ARG VITE_API_BASE_URL=/api/v1
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
