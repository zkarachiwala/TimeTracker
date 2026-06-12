# Session handoff — 2026-06-13

## Current branch
`fix/playwright-warmup` — all phases complete, ready for PR + merge

## Git state
- `fix/samesite-lax-oauth` — merged as PR #80 ✅
- `fix/playwright-warmup` — open branch, two commits ahead of main, ready to PR
- No uncommitted changes

---

## What was done this session

### Playwright test overhaul — COMPLETE ✅

All three phases done:

**Phase 1 — Test infrastructure**
- `GlobalSetup.cs` — `[SetUpFixture]` starts the app automatically (Release build, https profile)
  if not already running, then calls `/api/dev/login` via `APIRequestContext` to get a fresh auth
  cookie. No manual steps. No pre-generated tokens. `dotnet test` is the only command needed.
- `AuthSetup.cs` — deleted (fully replaced by `GlobalSetup.cs`)
- `AuthenticatedPageTest.cs`, `AuthenticatedDesktopPageTest.cs`, `AuthTests.cs` — cascade bug
  fixed (clear `_failedRequests` / `_consoleErrors` in `[SetUp]`), `IgnoreHTTPSErrors = true`
  restored on all contexts
- `NavigationTests.cs` — fixed all href selectors (Blazor renders `href="clients"` not
  `href="/clients"`), added auth-state wait in `[SetUp]` for admin-role links
- `TestConfig.cs` — default URL is `https://localhost:7006`
- `playwright.runsettings` — added with `StopOnError=true`
- **Full suite: 35 passed, 2 skipped (write tests) ✅**

**Phase 2 — Pre-push hook**
- Simplified: removed app-running check, removed `--filter "TestCategory!=Authenticated"`
- `GlobalSetup` handles startup automatically
- App-code change detection still present (skips for docs/test-only pushes)

**Phase 3 — CI**
- Replaced full Playwright job with a `smoke` job: curl production login page, assert HTTP 200
- Removed auth state restore step entirely
- `PLAYWRIGHT_AUTH_STATE_B64` GitHub secret should now be deleted from GitHub

---

## Next actions
1. Create PR from `fix/playwright-warmup` → `main`
2. After merge, delete `PLAYWRIGHT_AUTH_STATE_B64` secret from GitHub repository settings
3. Address Node.js 20 deprecation (GitHub forcing Node.js 24 from 2026-06-16 — see below)

## Node.js 20 deprecation — URGENT
GitHub Actions workflows use Node.js 20 actions. GitHub forcing Node.js 24 from 2026-06-16.
Must update before that date: `checkout@v4`, `setup-dotnet@v4`, `cache@v4`, `upload-artifact@v4`,
`upload-pages-artifact@v3`, `deploy-pages@v4`.

## Open GitHub issues (backlog)
- #36 — Invoice export for Zoho Books (future phase)
- #34 — App bar user avatar
- #32 — UX to recover soft-deleted records

## How to resume
```bash
cd /home/zkarachiwala/repos/TimeTracker
git status
cat SESSION.md
gh pr list
```

---
*Updated 2026-06-13. Replaces previous SESSION.md.*
