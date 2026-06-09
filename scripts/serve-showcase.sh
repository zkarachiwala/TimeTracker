#!/bin/bash
# Builds and serves the GitHub Pages showcase locally via nginx in Docker.
# Mirrors production: served at /TimeTracker/ with SPA fallback.
# Access at http://localhost:8080/TimeTracker/
#
# Usage: ./scripts/serve-showcase.sh
# Requires: Docker Desktop running, .NET 10 SDK

set -e

REPO_ROOT="$(git rev-parse --show-toplevel)"
OUTPUT_DIR="$REPO_ROOT/.showcase-local"
CONF_FILE="$OUTPUT_DIR/nginx.conf"
PORT=8080

echo "serve-showcase: publishing..."
dotnet publish "$REPO_ROOT/TimeTracker.Client/TimeTracker.Client.csproj" \
  --configuration Release \
  -p:Showcase=true \
  -p:DefineConstants=SHOWCASE \
  --output "$OUTPUT_DIR" \
  -v q

cat > "$CONF_FILE" <<'NGINX'
server {
    listen 80;
    location /TimeTracker/ {
        alias /usr/share/nginx/html/;
        try_files $uri $uri/ /TimeTracker/index.html;
    }
}
NGINX

echo "serve-showcase: starting nginx on http://localhost:$PORT/TimeTracker/"
docker run --rm \
  -p "$PORT:80" \
  -v "$OUTPUT_DIR/wwwroot:/usr/share/nginx/html:ro" \
  -v "$CONF_FILE:/etc/nginx/conf.d/default.conf:ro" \
  nginx:alpine
