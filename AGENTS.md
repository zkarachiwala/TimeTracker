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

**All work MUST be done in a `git worktree`.** Never run `git checkout`, `git reset`, or any branch-switching command in the main repository directory — it disrupts other agents (e.g. Claude Code) that may be working in the same repo. Use `git worktree add ../TimeTracker-issue-<N> -b feature/issue-<N>-<description>` to create an isolated working directory for every task.

## PR Merge Policy

Never merge a PR without explicit user instruction to do so. Confirming checks pass is not approval to merge. After opening a PR, stop and tell the user it's ready.

## Change Authorization

Never modify code without explicit user authorization. Planning, research, and questions are fine — but do not write, edit, or delete files until the user explicitly says to proceed. A conversational lead-in ("go", "let's go", "do it") is not authorization. The user must give a clear instruction like "make the change", "implement this", or "go ahead and build it."

This applies to all modes (planning, building, debugging, etc.). When in planning mode, present the plan and wait for explicit approval before writing any code.

## App Execution

Never run the application (`dotnet run`) or start any long-running process without explicit user instruction. Do not run `dotnet watch`, launch profiles, or any command that binds to ports or starts a server. Build and test commands are allowed, but anything that starts the app is not.

## Testing

- Run `dotnet test TimeTracker.sln --configuration Release` before every push. Never push with failing tests.
- Every service-layer change or new feature must include test additions/updates in the same commit.
- Check `TimeTracker.Tests` for relevant `*ServiceTests.cs` files before closing any task.
