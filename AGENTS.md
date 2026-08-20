# AirOps AI delivery rules

These rules apply to every coding agent working in this repository.

## Branches and phases

- Never develop directly on `main`.
- Start each phase on a branch named `codex/phase-<number>-<short-name>`.
- Keep all independently testable features for that phase on the same phase branch.
- A phase has one pull request. Additional feature commits are pushed to that pull request until the phase is complete.

## Feature completion

A feature is complete only when its scoped tests pass and its documentation is current.

After completing each independently testable feature:

1. Review the diff and exclude unrelated files, secrets, generated output, and local configuration.
2. Run the smallest relevant verification suite.
3. Commit the feature immediately using a Conventional Commit message, for example `feat(api): expose flight endpoints`.
4. Do not combine separate completed features in one commit.

Use `scripts/complete-feature.sh` to create the commit. Pass explicit paths whenever practical.

## Phase completion

A phase is complete only when all planned features are committed, the full verification suite passes, and the worktree is clean.

At phase completion:

1. Update the root README with the phase status and verification results.
2. Run `scripts/complete-phase.sh "Phase N: short description"`.
3. The script verifies the repository, pushes the phase branch, and creates or locates its GitHub pull request.
4. Report the commit SHA and pull-request URL to the user.

Do not merge a phase pull request unless the user explicitly asks.
