#!/usr/bin/env bash
# Container health check: asks the API for /api/v1/health and succeeds only on HTTP 200.
# It uses bash's built-in /dev/tcp, so the image needs no extra HTTP client such as curl.
set -euo pipefail

exec 3<>/dev/tcp/127.0.0.1/8080
printf 'GET /api/v1/health HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n' >&3
read -r status_line <&3

# The first line of the response looks like "HTTP/1.1 200 OK".
[[ "$status_line" == *" 200 "* ]]
