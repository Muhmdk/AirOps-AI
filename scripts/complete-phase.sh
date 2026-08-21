#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: scripts/complete-phase.sh \"Phase N: short description\" [base-branch]" >&2
}

if [[ $# -lt 1 || $# -gt 2 ]]; then
  usage
  exit 2
fi

title="$1"
base_branch="${2:-main}"
repo_root="$(git rev-parse --show-toplevel)"
branch="$(git branch --show-current)"
expected_github_user="$(git config --local --get airops.githubUser || true)"

if [[ ! "$branch" =~ ^codex/phase-[0-9]+-[a-z0-9._-]+$ ]]; then
  echo "Phase completion requires a branch named codex/phase-<number>-<short-name>." >&2
  exit 1
fi

if [[ -n "$(git status --porcelain)" ]]; then
  echo "The worktree must be clean before completing a phase." >&2
  git status --short >&2
  exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "GitHub CLI is required. Install gh and authenticate with: gh auth login" >&2
  exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "GitHub CLI is not authenticated. Run: gh auth login" >&2
  exit 1
fi

if [[ -z "$expected_github_user" ]]; then
  echo "Configure the permitted PR author with: git config --local airops.githubUser YOUR_GITHUB_LOGIN" >&2
  exit 1
fi

authenticated_github_user="$(gh api user --jq .login)"
if [[ "$authenticated_github_user" != "$expected_github_user" ]]; then
  echo "Authenticated GitHub user '$authenticated_github_user' does not match required PR author '$expected_github_user'." >&2
  exit 1
fi

if printf '%s\n%s\n' "$title" "$authenticated_github_user" | grep -Eiq 'codex|openai|chatgpt'; then
  echo "AI or bot attribution is not permitted in phase PR authorship or titles." >&2
  exit 1
fi

cd "$repo_root"
scripts/verify.sh
git push --set-upstream origin "$branch"

existing_url="$(gh pr view "$branch" --json url --jq .url 2>/dev/null || true)"
if [[ -n "$existing_url" ]]; then
  echo "Phase pull request already exists: $existing_url"
  exit 0
fi

pr_url="$(gh pr create --base "$base_branch" --head "$branch" --title "$title" --fill)"
echo "Phase pull request created: $pr_url"
