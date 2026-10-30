FROM node:20-alpine AS viewer
WORKDIR /viewer
COPY viewer/package*.json ./
RUN npm ci
COPY viewer/ .
RUN npx vite build --base=/viewer/ --outDir /out --emptyOutDir

FROM nginx:alpine
COPY deploy/nginx.conf /etc/nginx/conf.d/default.conf
COPY frontend/ /usr/share/nginx/html/frontend/
COPY --from=viewer /out /usr/share/nginx/html/viewer/