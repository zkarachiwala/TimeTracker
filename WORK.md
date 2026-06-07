# Work Log

This file tracks active work, current state, and next steps. Updated before every commit and at the end of every session. The source of truth for resuming work after a context reset.

## Current branch
`feature/wasm-islands`

## Current state (2026-06-07)

Large amount of uncommitted work from migration to global InteractiveWebAssembly. Before working on any issues below, this must be dealt with.

### What is uncommitted
- `TimeTracker.Client/` — entire new WASM project (untracked, never committed)
- Deletion of `TimeTracker.Wasm/` — the old project name
- Modifications to `TimeTracker.Web/App.razor`, `Program.cs`, `_Imports.razor`
- New/modified Playwright tests
- New unit tests for Reports calculations

### Known broken state
The code currently has the bugs listed in the issues below. Do not run Playwright until issues #57 and #58 are fixed — tests will fail due to 500s and missing login page.

---

## Open issues (this branch)

Work through these in order. Each issue = one PR or one commit minimum.

| # | Title | Status |
|---|-------|--------|
| [#57](https://github.com/zkarachiwala/TimeTracker/issues/57) | bug: duplicate @page routes cause 500 on hard refresh | **Next** |
| [#58](https://github.com/zkarachiwala/TimeTracker/issues/58) | bug: login and access-denied pages not reachable from WASM Router | Todo |
| [#59](https://github.com/zkarachiwala/TimeTracker/issues/59) | security: ProjectEndpoints missing Admin role on mutations | Todo |
| [#60](https://github.com/zkarachiwala/TimeTracker/issues/60) | refactor: ITimeEntryService contract violation (NotSupportedException) | Todo |
| [#61](https://github.com/zkarachiwala/TimeTracker/issues/61) | test: architectural test for duplicate @page routes | Todo |

---

## How to resume after context reset

```bash
git status          # see what is uncommitted
git log --oneline   # see last committed state
cat WORK.md         # read this file
gh issue list --state open  # check issue status
```

Then read the current issue's GitHub description and continue from **Next** in the table above.

---

## Session log

### 2026-06-07
- Code review completed against .NET 10 Blazor best practices
- Found 4 duplicate routes, security gap in ProjectEndpoints, login page routing bug, interface contract violation
- Created GitHub issues #57–#61
- Added git discipline rule to CLAUDE.md
- No commits made this session — large uncommitted pile exists (see above)
