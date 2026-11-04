#!/usr/bin/env bash
# Uploads the built React app to the web bucket and clears index.html from CloudFront's cache.
# Files in assets/ have a content hash in their name, so browsers may keep them for a year.
# Everything else (index.html, favicon, ...) must be re-checked on every visit.
#
# Usage: deploy-frontend.sh <dist folder> <web bucket name> <CloudFront distribution id>
set -euo pipefail

dist="${1:?Pass the dist folder}"
bucket="${2:?Pass the web bucket name}"
distribution="${3:?Pass the CloudFront distribution id}"

if [ -d "$dist/assets" ]; then
  aws s3 sync "$dist/assets" "s3://$bucket/assets" --cache-control "public, max-age=31536000, immutable"
fi
aws s3 sync "$dist" "s3://$bucket" --exclude "assets/*" --cache-control "no-cache"

# Remove files that are no longer part of the build.
aws s3 sync "$dist" "s3://$bucket" --delete --size-only

aws cloudfront create-invalidation --distribution-id "$distribution" --paths "/index.html" > /dev/null
echo "Uploaded to s3://$bucket and invalidated /index.html"
