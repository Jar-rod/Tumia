#!/usr/bin/env bash
# Stops or starts the parts of the AWS environment that cost money by the hour, so it only costs money while
# someone is using it.
#   stop            scales the API to 0 tasks, then stops the database
#   start           starts the database, waits until it is ready, then runs 1 API task again
#   start-database  starts the database only (deploy.yml runs this before every deploy)
#   status          shows the state of the database and the API
# The load balancer cannot be stopped: it keeps costing about US$0.65 a day while everything else is off.
# AWS starts a stopped database again by itself after 7 days.
#
# Usage: environment.sh <stop|start|start-database|status> <environment name>
set -euo pipefail

action="${1:?Pass stop, start, start-database or status}"
env_name="${2:?Pass the environment name, for example prod}"
cluster="$env_name-tumiia"
service="$env_name-tumiia-api"
script_dir="$(dirname "$0")"

stack_output() {
  aws cloudformation describe-stacks --stack-name "$env_name-tumiia-$1" \
    --query "Stacks[0].Outputs[?OutputKey=='$2'].OutputValue | [0]" --output text
}

database_status() {
  aws rds describe-db-instances --db-instance-identifier "$1" --query 'DBInstances[0].DBInstanceStatus' --output text
}

# Waits up to 30 minutes for the database to reach the wanted status ("available" or "stopped").
wait_for_database() {
  local database="$1" wanted="$2" status
  for _ in $(seq 1 90); do
    status="$(database_status "$database")"
    if [ "$status" = "$wanted" ]; then
      return 0
    fi
    echo "Database is $status; waiting until it is $wanted"
    sleep 20
  done
  echo "::error::The database did not become $wanted within 30 minutes."
  exit 1
}

stop_database() {
  local database="$1" status
  status="$(database_status "$database")"
  case "$status" in
    stopped) echo "The database is already stopped." ;;
    stopping) wait_for_database "$database" stopped ;;
    *)
      # A database that is busy (starting, backing up) cannot be stopped until it is available.
      wait_for_database "$database" available
      echo "Stopping the database"
      aws rds stop-db-instance --db-instance-identifier "$database" > /dev/null
      wait_for_database "$database" stopped
      ;;
  esac
}

start_database() {
  local database="$1" status
  status="$(database_status "$database")"
  case "$status" in
    available) echo "The database is already running." ;;
    stopped | stopping)
      wait_for_database "$database" stopped
      echo "Starting the database (this takes a few minutes)"
      aws rds start-db-instance --db-instance-identifier "$database" > /dev/null
      wait_for_database "$database" available
      ;;
    *) wait_for_database "$database" available ;;
  esac
}

scale_api() {
  echo "Setting the API to $1 task(s)"
  aws ecs update-service --cluster "$cluster" --service "$service" --desired-count "$1" > /dev/null
  aws ecs wait services-stable --cluster "$cluster" --services "$service"
}

show_status() {
  local database="$1" counts address
  counts="$(aws ecs describe-services --cluster "$cluster" --services "$service" \
    --query 'services[0].[desiredCount,runningCount]' --output text)"
  address="$(stack_output web DistributionDomainName)"
  {
    echo "### $env_name environment"
    echo "- Database: $(database_status "$database")"
    echo "- API tasks (wanted / running): ${counts//$'\t'/ / }"
    echo "- App address: https://$address"
  } | tee -a "${GITHUB_STEP_SUMMARY:-/dev/null}"
}

database="$(stack_output data DatabaseInstanceId)"
if [ -z "$database" ] || [ "$database" = "None" ]; then
  if [ "$action" = "start-database" ]; then
    echo "The data stack does not report a database id yet (it will after the next infra deploy); skipping."
    exit 0
  fi
  echo "::error::Could not find the database id in the $env_name-tumiia-data stack outputs. Run Deploy with target infra first."
  exit 1
fi

case "$action" in
  stop)
    scale_api 0
    stop_database "$database"
    show_status "$database"
    ;;
  start)
    start_database "$database"
    if [ -n "$("$script_dir/current-image-tag.sh" "$env_name")" ]; then
      scale_api 1
    else
      echo "::warning::No API image has been deployed yet, so the API was not started. Run Deploy with target backend."
    fi
    show_status "$database"
    ;;
  start-database) start_database "$database" ;;
  status) show_status "$database" ;;
  *) echo "::error::Unknown action '$action'. Use stop, start, start-database or status."; exit 1 ;;
esac
