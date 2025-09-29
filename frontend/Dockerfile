# =========================================
# Stage 1: Build the Angular Application
# =========================================

ARG NODE_VERSION=24.8.0
ARG NGINX_VERSION=alpine3.22

# Use node image for base image for all stages. Alpine because it's lightweight
FROM node:${NODE_VERSION}-alpine as builder

# Set working directory for all build stages.
WORKDIR /app

# Copy package-related files first to leverage Docker's caching mechanism
COPY package.json package-lock.json ./

# Install project dependencies using npm ci (ensures a clean, reproducible install)
RUN --mount=type=cache,target=/root/.npm npm ci

# Copy the rest of the application source code into the container
COPY . .

# Build Angular app
RUN npm run build

# =========================================
# Stage 2: Prepare Nginx to Serve Static Files
# =========================================

FROM nginxinc/nginx-unprivileged:${NGINX_VERSION} AS runner

# Use a built-in non-root user for security best practices
USER nginx

# Copy custom Nginx config
COPY nginx.conf /etc/nginx/nginx.conf

# Copy the static build output from the build stage to Nginx's default HTML serving directory
COPY --chown=nginx:nginx --from=builder /app/dist/*/browser /usr/share/nginx/html

# Expose port 8443 to allow HTTPS traffic
# Note: The default NGINX container now listens on port 8443 instead of 80
EXPOSE 8443

# Start Nginx directly with custom config
ENTRYPOINT ["nginx", "-c", "/etc/nginx/nginx.conf"]
CMD ["-g", "daemon off;"]
