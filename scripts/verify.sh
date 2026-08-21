#!/usr/bin/env bash
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
web_dir="$repo_root/apps/web-angular"
api_tests="$repo_root/apps/api/AirOps.Api.Tests/AirOps.Api.Tests.csproj"

if [[ -f "$api_tests" ]]; then
  if ! command -v dotnet >/dev/null 2>&1; then
    echo "The .NET SDK is required to verify apps/api." >&2
    exit 1
  fi
  echo "Running ASP.NET Core API tests..."
  dotnet test "$api_tests" --nologo
  echo "Checking Entity Framework migration alignment..."
  dotnet tool restore
  dotnet ef migrations has-pending-model-changes \
    --project "$repo_root/apps/api/AirOps.Api/AirOps.Api.csproj" \
    --startup-project "$repo_root/apps/api/AirOps.Api/AirOps.Api.csproj" \
    --no-build
fi

if [[ -f "$web_dir/pnpm-lock.yaml" ]] && command -v pnpm >/dev/null 2>&1; then
  test_command=(pnpm exec ng test --watch=false)
  build_command=(pnpm exec ng build)
elif [[ -f "$web_dir/package.json" ]] && command -v npm >/dev/null 2>&1; then
  test_command=(npm test -- --watch=false)
  build_command=(npm run build)
else
  echo "Unable to find pnpm or npm for apps/web-angular." >&2
  exit 1
fi

echo "Running Angular unit tests..."
(
  cd "$web_dir"
  "${test_command[@]}"
)

echo "Building Angular production bundle..."
(
  cd "$web_dir"
  "${build_command[@]}"
)

echo "Repository verification passed."
