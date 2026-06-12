# Session handoff — 2026-06-13

## Current state
- Branch: `main`, clean, no uncommitted changes
- All CI green, Dependabot active (grouped updates configured by user)

## What was completed this session
- **PR #80** — `SameSite=Lax` fix for iOS OAuth login loop ✅
- **PR #81** — Playwright overhaul: automated auth via `/api/dev/login`, GlobalSetup auto-starts app, fixed nav selectors, smoke-test CI, pre-push hook simplified. Full suite: 35 passed, 2 skipped ✅
- **PR #82** — GitHub Actions bumped to Node.js 24 compatible versions; Dependabot added ✅
- **PR #84** — `mockup/` marked as linguist-vendored (suppresses CodeQL JS detection) ✅
- **`PLAYWRIGHT_AUTH_STATE_B64`** GitHub secret deleted ✅

## Backlog (open GitHub issues)
- **#36** — Invoice export for Zoho Books (future phase, keep REST API layer intact)
- **#34** — App bar user avatar
- **#32** — UX to recover soft-deleted records

## How to resume
```bash
cd /home/zkarachiwala/repos/TimeTracker
git status
cat SESSION.md
gh pr list
```

---
*Updated 2026-06-13. Replaces previous SESSION.md.*
