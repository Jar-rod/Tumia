#!/usr/bin/env bash
# Runs a CDK command for every stack, passing the deployment settings from environment variables
# (set from the GitHub "prod" environment variables in deploy.yml).
#
# Usage: cdk.sh <diff|deploy>   (run from the infra/ folder)
# Required: ENV_NAME, AWS_ACCOUNT_ID, REPO_OIDC_SUBJECT, ALERT_EMAIL, CLOUDFRONT_PREFIX_LIST_ID
# Optional: ENABLE_SWAGGER ("true" serves Swagger UI at /swagger), DOMAIN_NAME with HOSTED_ZONE_ID and HOSTED_ZONE_NAME (without them the app uses its CloudFront
#           address), SENDER_EMAIL (used when there is no domain), IMAGE_TAG, ADMIN_EMAILS, ENABLE_BEDROCK
#           ("false" stops sending documents to Bedrock; on by default), BEDROCK_MODEL_ID (default anthropic.claude-haiku-5-5)
set -euo pipefail

command="${1:?Pass diff or deploy}"
cdk_version="2.1147.0"

context=(
  -c "env=${ENV_NAME:?}"
  -c "account=${AWS_ACCOUNT_ID:?}"
  -c "githubRepo=${REPO_OIDC_SUBJECT:?}"
  -c "alertEmail=${ALERT_EMAIL:?}"
  -c "cloudFrontPrefixListId=${CLOUDFRONT_PREFIX_LIST_ID:?}"
)

# Without a domain, users open the CloudFront address and email comes from SENDER_EMAIL (or ALERT_EMAIL).
if [ -n "${DOMAIN_NAME:-}" ]; then
  context+=(
    -c "domainName=$DOMAIN_NAME"
    -c "hostedZoneId=${HOSTED_ZONE_ID:?DOMAIN_NAME also needs HOSTED_ZONE_ID}"
    -c "hostedZoneName=${HOSTED_ZONE_NAME:-$DOMAIN_NAME}"
  )
fi

if [ -n "${SENDER_EMAIL:-}" ]; then
  context+=(-c "senderEmail=$SENDER_EMAIL")
fi

# Keep the image the service is already running; otherwise CDK would roll the API back to an older image.
if [ -n "${IMAGE_TAG:-}" ]; then
  context+=(-c "imageTag=$IMAGE_TAG")
fi

# Comma-separated emails that get the Admin role.
if [ -n "${ADMIN_EMAILS:-}" ]; then
  context+=(-c "adminEmails=$ADMIN_EMAILS")
fi

if [ "${ENABLE_SWAGGER:-false}" = "true" ]; then
  context+=(-c "enableSwagger=true")
fi

# Documents are read by Claude on Amazon Bedrock (processed outside South Africa) unless ENABLE_BEDROCK=false.
if [ "${ENABLE_BEDROCK:-true}" = "true" ]; then
  context+=(-c "enableBedrock=true")
fi

if [ -n "${BEDROCK_MODEL_ID:-}" ]; then
  context+=(-c "bedrockModelId=$BEDROCK_MODEL_ID")
fi

case "$command" in
  diff) npx --yes "aws-cdk@$cdk_version" diff --all "${context[@]}" ;;
  deploy) npx --yes "aws-cdk@$cdk_version" deploy --all --require-approval never "${context[@]}" ;;
  *) echo "::error::Unknown command '$command'. Use diff or deploy."; exit 1 ;;
esac
