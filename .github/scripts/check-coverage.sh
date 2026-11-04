#!/usr/bin/env bash
# Fails when line coverage is below the minimum.
#
# Usage: check-coverage.sh <path to ReportGenerator Summary.json> <minimum percent>
# Example: check-coverage.sh services/api/TestResults/report/Summary.json 70
set -euo pipefail

summary_file="${1:?Pass the path to Summary.json}"
minimum="${2:?Pass the minimum line coverage, for example 70}"

if [ ! -f "$summary_file" ]; then
  echo "::error::Coverage summary not found at $summary_file. Did the tests run?"
  exit 1
fi

line_coverage="$(jq -r '.summary.linecoverage' "$summary_file")"

# awk compares the numbers, because bash can only compare whole numbers.
if awk -v actual="$line_coverage" -v required="$minimum" 'BEGIN { exit !(actual < required) }'; then
  echo "::error::Line coverage is ${line_coverage}%, below the ${minimum}% minimum. Add tests for the code you changed."
  exit 1
fi

echo "Line coverage is ${line_coverage}% (minimum ${minimum}%)."
