#!/bin/sh#!/bin/bash#!/bin/bash



# Start nginx with the generated configset -eset -e

echo "Starting with PORT: $PORT"

sed "s/PORT/$PORT/g" /app/nginx.conf.template > /etc/nginx/nginx.conf

nginx -t || exit 1

# Set default port# Set default port

# Start .NET backend (runs in background)

/app/backend/Api &export PORT=${PORT:-8080}export PORT=${PORT:-8080}



# Wait for .NET backend to start (optional, can be improved)echo "Starting with PORT: $PORT"echo "Starting with PORT: $PORT"

sleep 5



# Start nginx (reverse proxy)

echo "Starting nginx on port $PORT"# Create nginx config with port substitution

nginx -g 'daemon off;'
sed "s/HEROKU_PORT/$PORT/g" /app/nginx.conf.template > /etc/nginx/nginx.conf# Create nginx config with port substitution (правильний шлях)

sed "s/HEROKU_PORT/$PORT/g" /app/nginx.conf.template > /etc/nginx/nginx.conf

# Test nginx config

nginx -t# Test nginx config

nginx -t

# Start .NET backend in background on port 5000

export ASPNETCORE_URLS="http://0.0.0.0:5000"# Start .NET backend in background on port 5000

echo "Starting .NET API on port 5000"export ASPNETCORE_URLS="http://0.0.0.0:5000"

dotnet /app/backend/Api.dll &echo "Starting .NET API on port 5000"

dotnet /app/backend/Api.dll &

# Wait a moment for backend to start

sleep 2# Wait a moment for backend to start

sleep 2

# Start nginx in foreground

echo "Starting nginx on port $PORT"# Start nginx in foreground

nginx -g "daemon off;"echo "Starting nginx on port $PORT"

nginx -g "daemon off;"