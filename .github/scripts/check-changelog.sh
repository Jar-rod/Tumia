#!/usr/bin/env bash
# Fails when code changed but CHANGELOG.md did not (see docs/CONTRIBUTING.md).
# "Code" means anything under apps/, services/, infra/ or .github/.
#
# Usage: check-changelog.sh <base commit> <head commit>
# Set SKIP_LABEL=true to pass anyway (the pull request has the "no-changelog" label).
set -euo pipefail

base="${1:-}"
head="${2:?Pass the head commit}"
no_previous_commit="0000000000000000000000000000000000000000"

if [ "${SKIP_LABEL:-false}" = "true" ]; then
  echo "The pull request has the 'no-changelog' label, so CHANGELOG.md is not required."
  exit 0
fi

if [ -z "$base" ] || [ "$base" = "$no_previous_commit" ]; then
  echo "No earlier commit to compare with, so there is nothing to check."
  exit 0
fi

# Three dots: only the changes made on the head side since it branched off the base.
changed_files="$(git diff --name-only "$base...$head")"
code_changes="$(echo "$changed_files" | grep -E '^(apps|services|infra|\.github)/' || true)"
changelog_changed="$(echo "$changed_files" | grep -Fx 'CHANGELOG.md' || true)"

if [ -z "$code_changes" ]; then
  echo "No code changed, so CHANGELOG.md is not required."
  exit 0
fi

if [ -z "$changelog_changed" ]; then
  echo "::error::Code changed but CHANGELOG.md did not. Add a line under '## [Unreleased]', or add the 'no-changelog' label for a pure refactor."
  echo "Code files changed:"
  echo "$code_changes"
  exit 1
fi

echo "CHANGELOG.md was updated."
