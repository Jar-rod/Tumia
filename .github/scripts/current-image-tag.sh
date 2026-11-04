#!/usr/bin/env bash
# Prints the image tag the API service is running now, or nothing before the first backend deploy.
#
# Usage: current-image-tag.sh <environment name>
set -euo pipefail

env_name="${1:?Pass the environment name, for example prod}"
cluster="$env_name-tumiia"
service="$env_name-tumiia-api"

task_definition="$(aws ecs describe-services --cluster "$cluster" --services "$service" \
  --query "services[?status=='ACTIVE'].taskDefinition | [0]" --output text 2>/dev/null || true)"

if [ -z "$task_definition" ] || [ "$task_definition" = "None" ]; then
  exit 0
fi

image="$(aws ecs describe-task-definition --task-definition "$task_definition" \
  --query "taskDefinition.containerDefinitions[?name=='api'].image | [0]" --output text)"
tag="${image##*:}"

# "not-deployed-yet" is the placeholder CDK uses before any image exists.
if [ "$tag" != "not-deployed-yet" ]; then
  echo "$tag"
fi
