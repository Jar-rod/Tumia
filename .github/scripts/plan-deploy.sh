#!/usr/bin/env bash
# Works out what the deploy workflow should deploy, and writes frontend=, backend= and infra= (true/false)
# to $GITHUB_OUTPUT.
#
# Usage: plan-deploy.sh <auto|frontend|backend|both|infra>
#
# "auto" compares HEAD with the latest deploy tag of each part (deploy-frontend-*, deploy-backend-*):
#   - apps/web changed      -> deploy the frontend
#   - services/api changed  -> deploy the backend (code or Dockerfile)
#   - no tag yet            -> deploy that part
#   - infra changed         -> never deployed by auto; prints a warning to run "target: infra"
set -euo pipefail

target="${1:?Pass the target: auto, frontend, backend, both or infra}"
output_file="${GITHUB_OUTPUT:-/dev/stdout}"
summary_file="${GITHUB_STEP_SUMMARY:-/dev/stdout}"

# Newest deploy tag for one part. Tag names end in yyyyMMdd-HHmm-sha, so sorting by name is sorting by time.
latest_tag() {
  git tag --list "deploy-$1-*" --sort=-refname | head -n 1
}

# True when the folder changed since the tag, or when there is no tag yet.
changed_since() {
  local tag="$1" folder="$2"
  if [ -z "$tag" ]; then
    return 0
  fi
  ! git diff --quiet "$tag" HEAD -- "$folder"
}

frontend=false
backend=false
infra=false

case "$target" in
  frontend) frontend=true ;;
  backend) backend=true ;;
  both) frontend=true; backend=true ;;
  infra) infra=true ;;
  auto)
    if changed_since "$(latest_tag frontend)" apps/web; then frontend=true; fi
    if changed_since "$(latest_tag backend)" services/api; then backend=true; fi
    if changed_since "$(latest_tag infra)" infra; then
      echo "::warning::infra/ has changed since the last infrastructure deploy. Auto never deploys infrastructure: run this workflow again with target: infra."
    fi
    ;;
  *)
    echo "::error::Unknown target '$target'. Use auto, frontend, backend, both or infra."
    exit 1
    ;;
esac

{
  echo "frontend=$frontend"
  echo "backend=$backend"
  echo "infra=$infra"
} >> "$output_file"

{
  echo "### Deploy plan (target: $target)"
  echo "| Part | Deploy? |"
  echo "|---|---|"
  echo "| frontend | $frontend |"
  echo "| backend | $backend |"
  echo "| infra | $infra |"
} >> "$summary_file"
