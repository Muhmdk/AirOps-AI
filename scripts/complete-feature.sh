#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: scripts/complete-feature.sh \"type(scope): message\" [--all | -- path ...]" >&2
}

if [[ $# -lt 1 ]]; then
  usage
  exit 2
fi

message="$1"
shift
repo_root="$(git rev-parse --show-toplevel)"
branch="$(git branch --show-current)"

if [[ "$branch" == "main" || "$branch" == "master" || -z "$branch" ]]; then
  echo "Feature commits must be created on a phase branch, not $branch." >&2
  exit 1
fi

if [[ ! "$message" =~ ^(feat|fix|test|docs|refactor|perf|build|ci|chore)(\([a-z0-9._-]+\))?!?:[[:space:]].+ ]]; then
  echo "Commit message must follow Conventional Commits." >&2
  usage
  exit 2
fi

cd "$repo_root"
if [[ ${1:-} == "--all" ]]; then
  git add -A
elif [[ ${1:-} == "--" ]]; then
  shift
  if [[ $# -eq 0 ]]; then
    usage
    exit 2
  fi
  git add -- "$@"
elif [[ $# -gt 0 ]]; then
  usage
  exit 2
fi

if git diff --cached --quiet; then
  echo "Nothing is staged. Pass explicit paths after --, or use --all after reviewing the diff." >&2
  exit 1
fi

if git diff --cached --name-only | grep -E '(^|/)(\.env($|\.)|node_modules/|dist/|coverage/)|\.(pem|key|p12)$' >/dev/null; then
  echo "Refusing to commit likely secrets or generated output." >&2
  exit 1
fi

git diff --cached --check
git commit -m "$message"

echo "Feature committed on $branch at $(git rev-parse --short HEAD)."
