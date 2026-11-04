#!/usr/bin/env bash
# Runs a CDK command for every stack, passing the deployment settings from environment variables
# (set from the GitHub "prod" environment variables in deploy.yml).
#
# Usage: cdk.sh <diff|deploy>   (run from the infra/ folder)
# Required: ENV_NAME, AWS_ACCOUNT_ID, DOMAIN_NAME, HOSTED_ZONE_ID, HOSTED_ZONE_NAME, REPO_OIDC_SUBJECT,
#           ALERT_EMAIL, CLOUDFRONT_PREFIX_LIST_ID
# Optional: IMAGE_TAG, ADMIN_EMAILS, ENABLE_BEDROCK, BEDROCK_INFERENCE_PROFILE_ARN, BEDROCK_MODEL_ID
set -euo pipefail

command="${1:?Pass diff or deploy}"
cdk_version="2.1147.0"

context=(
  -c "env=${ENV_NAME:?}"
  -c "account=${AWS_ACCOUNT_ID:?}"
  -c "domainName=${DOMAIN_NAME:?}"
  -c "hostedZoneId=${HOSTED_ZONE_ID:?}"
  -c "hostedZoneName=${HOSTED_ZONE_NAME:?}"
  -c "githubRepo=${REPO_OIDC_SUBJECT:?}"
  -c "alertEmail=${ALERT_EMAIL:?}"
  -c "cloudFrontPrefixListId=${CLOUDFRONT_PREFIX_LIST_ID:?}"
)

# Keep the image the service is already running; otherwise CDK would roll the API back to an older image.
if [ -n "${IMAGE_TAG:-}" ]; then
  context+=(-c "imageTag=$IMAGE_TAG")
fi

# Comma-separated emails that get the Admin role.
if [ -n "${ADMIN_EMAILS:-}" ]; then
  context+=(-c "adminEmails=$ADMIN_EMAILS")
fi

if [ "${ENABLE_BEDROCK:-false}" = "true" ]; then
  context+=(
    -c "enableBedrock=true"
    -c "bedrockInferenceProfileArn=${BEDROCK_INFERENCE_PROFILE_ARN:?}"
    -c "bedrockModelId=${BEDROCK_MODEL_ID:?}"
  )
fi

case "$command" in
  diff) npx --yes "aws-cdk@$cdk_version" diff --all "${context[@]}" ;;
  deploy) npx --yes "aws-cdk@$cdk_version" deploy --all --require-approval never "${context[@]}" ;;
  *) echo "::error::Unknown command '$command'. Use diff or deploy."; exit 1 ;;
esac
