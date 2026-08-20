#!/usr/bin/env bash
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
web_dir="$repo_root/apps/web-angular"

if [[ -f "$web_dir/pnpm-lock.yaml" ]] && command -v pnpm >/dev/null 2>&1; then
  package_runner=(pnpm)
elif [[ -f "$web_dir/package.json" ]] && command -v npm >/dev/null 2>&1; then
  package_runner=(npm)
else
  echo "Unable to find pnpm or npm for apps/web-angular." >&2
  exit 1
fi

echo "Running Angular unit tests..."
(
  cd "$web_dir"
  "${package_runner[@]}" test -- --watch=false
)

echo "Building Angular production bundle..."
(
  cd "$web_dir"
  "${package_runner[@]}" run build
)

echo "Repository verification passed."
