# Feature commits and phase pull requests

AirOps AI uses one branch and one pull request per development phase. Every independently testable feature within that phase is committed separately.

## Start a phase

```bash
git switch main
git pull --ff-only
git switch -c codex/phase-5-ml-risk-model
```

## Complete a feature

Review the feature diff, run its relevant tests, then commit only its files:

```bash
scripts/complete-feature.sh "feat(ml): add training data generator" -- data/generators apps/ml-service
```

If the correct files are already staged:

```bash
scripts/complete-feature.sh "test(ml): cover prediction fallback"
```

`--all` is available, but should only be used after reviewing the entire worktree.

## Complete a phase

First authenticate the GitHub CLI once on the workstation:

```bash
gh auth login
```

When all phase features are committed and the worktree is clean:

```bash
scripts/complete-phase.sh "Phase 5: Machine-learning risk model"
```

The phase script runs the full repository verification, pushes the current phase branch, and creates a pull request into `main`. If a pull request already exists, it reports the existing URL instead of creating a duplicate.

GitHub Actions repeats the Angular unit tests and production build for every web pull request.
