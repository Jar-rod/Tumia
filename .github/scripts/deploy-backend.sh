#!/usr/bin/env bash
# Rolls out a new API image on ECS:
#   1. registers a new task definition revision that uses the new image
#   2. runs the database migrations as a one-off task (/app/efbundle) and stops if they fail
#   3. points the service at the new revision and waits until it is stable
# If the new version fails its health checks, ECS rolls back to the previous one by itself.
#
# Usage: deploy-backend.sh <environment name> <full image URI with tag>
set -euo pipefail

env_name="${1:?Pass the environment name, for example prod}"
image="${2:?Pass the image URI, for example 123456789012.dkr.ecr.af-south-1.amazonaws.com/prod-tumiia-api:abc1234}"
cluster="$env_name-tumiia"
service="$env_name-tumiia-api"
container="api"
log_group="/ecs/$env_name-tumiia-api"

service_json="$(aws ecs describe-services --cluster "$cluster" --services "$service" --query 'services[0]')"
current_task_definition="$(jq -r '.taskDefinition' <<< "$service_json")"
network_configuration="$(jq -c '.networkConfiguration' <<< "$service_json")"
current_desired_count="$(jq -r '.desiredCount' <<< "$service_json")"

register_task_definition() {
  echo "Registering a task definition that runs $image" >&2
  aws ecs describe-task-definition --task-definition "$current_task_definition" --query taskDefinition \
    | jq --arg image "$image" --arg container "$container" '
        (.containerDefinitions[] | select(.name == $container) | .image) = $image
        | del(.taskDefinitionArn, .revision, .status, .requiresAttributes, .compatibilities,
              .registeredAt, .registeredBy, .deregisteredAt)' \
    > task-definition.json
  aws ecs register-task-definition --cli-input-json file://task-definition.json \
    --query taskDefinition.taskDefinitionArn --output text
}

run_migrations() {
  local task_definition="$1"
  echo "Running database migrations"
  local task_arn
  task_arn="$(aws ecs run-task --cluster "$cluster" --launch-type FARGATE \
    --task-definition "$task_definition" \
    --network-configuration "$network_configuration" \
    --overrides "{\"containerOverrides\":[{\"name\":\"$container\",\"command\":[\"/app/efbundle\"]}]}" \
    --started-by github-deploy-migrations \
    --query 'tasks[0].taskArn' --output text)"

  aws ecs wait tasks-stopped --cluster "$cluster" --tasks "$task_arn"

  local exit_code
  exit_code="$(aws ecs describe-tasks --cluster "$cluster" --tasks "$task_arn" \
    --query "tasks[0].containers[?name=='$container'].exitCode | [0]" --output text)"
  if [ "$exit_code" != "0" ]; then
    echo "::error::Database migrations failed (exit code $exit_code). Nothing was deployed. Last log lines:"
    aws logs get-log-events --log-group-name "$log_group" --log-stream-name "api/$container/${task_arn##*/}" \
      --limit 50 --query 'events[].message' --output text || true
    exit 1
  fi
  echo "Migrations finished"
}

update_service() {
  local task_definition="$1"
  # Before the first deploy the service runs zero tasks; from now on it runs at least one.
  local desired_count=$(( current_desired_count > 0 ? current_desired_count : 1 ))
  echo "Updating the service to $task_definition ($desired_count task(s))"
  aws ecs update-service --cluster "$cluster" --service "$service" \
    --task-definition "$task_definition" --desired-count "$desired_count" > /dev/null
  aws ecs wait services-stable --cluster "$cluster" --services "$service"
  echo "Service is stable"
}

new_task_definition="$(register_task_definition)"
run_migrations "$new_task_definition"
update_service "$new_task_definition"
