# Work Log

## How to resume
```bash
git status
git log --oneline -5
cat WORK.md
gh issue view <number>   # full technical spec for the active issue
```

## Active branch
`feature/wasm-islands`

## Active issue
**None — all issues resolved. Ready to merge `feature/wasm-islands` → `main`.**

## Issue queue

| # | Title | Status |
|---|-------|--------|
| [#57](https://github.com/zkarachiwala/TimeTracker/issues/57) | Duplicate @page routes — 500 on hard refresh | Done `a574ab4` |
| [#58](https://github.com/zkarachiwala/TimeTracker/issues/58) | Login/access-denied not reachable from WASM Router | Done `a4ab148` |
| [#59](https://github.com/zkarachiwala/TimeTracker/issues/59) | ProjectEndpoints missing Admin role on mutations | Done `642575f` |
| [#60](https://github.com/zkarachiwala/TimeTracker/issues/60) | ITimeEntryService NotSupportedException contract violation | Done `201413b` |
| [#61](https://github.com/zkarachiwala/TimeTracker/issues/61) | Architectural test for duplicate @page routes | Done `f639788` |

## Rules
- Every issue has a complete technical spec on GitHub. WORK.md is the index only.
- Commit after each issue. Update this file before committing.
- Do not start an issue without reading its GitHub description first.
- "Done" means committed and this file updated.
