#!/bin/sh

# Start nginx with the generated config
echo "Starting with PORT: $PORT"
sed "s/PORT/$PORT/g" /app/nginx.conf.template > /etc/nginx/nginx.conf
nginx -t || exit 1

# Start .NET backend (runs in background)
/app/backend/Api &

# Wait for .NET backend to start (optional, can be improved)
sleep 5

# Start nginx (reverse proxy)
echo "Starting nginx on port $PORT"
nginx -g 'daemon off;'