# opencode Project Instructions

This file contains opencode-specific rules for the TimeTracker project.
CLAUDE.md is loaded separately via `opencode.json` instructions — both apply.

## Git Workflow

All changes must follow this workflow:
1. Create a GitHub issue on the project board at https://github.com/users/zkarachiwala/projects/1
2. Create a feature branch tied to that issue
3. Do all work on the branch — never commit directly to `main`
4. Open a PR and link it to the issue
5. Merge via PR only — never push directly to `main`
6. When a PR is merged: pull `main`, delete the local feature branch

## PR Merge Policy

Never merge a PR without explicit user instruction to do so. Confirming checks pass is not approval to merge. After opening a PR, stop and tell the user it's ready.

## Testing

- Run `dotnet test TimeTracker.sln --configuration Release` before every push. Never push with failing tests.
- Every service-layer change or new feature must include test additions/updates in the same commit.
- Check `TimeTracker.Tests` for relevant `*ServiceTests.cs` files before closing any task.
