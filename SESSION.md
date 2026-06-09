# Session handoff — 2026-06-09

## Current branch
`main` — clean and up to date

## Git state
- All PRs from this session merged: #68, #69
- No open PRs
- No uncommitted changes

## What was done this session

### Playwright CI auth — fixed (PR #68)

Root cause of 29 authenticated test failures in CI was a cookie domain mismatch: auth state was captured locally (`domain: localhost`) but CI runs against `timetracker-zak.azurewebsites.net`. Three options were reviewed with a security audit; a production auth bypass endpoint was rejected.

**Decision (D010):** Authenticated Playwright tests excluded from CI. 9 unauthenticated tests run in CI; 29 authenticated tests skipped (not failed) via `Assert.Ignore()` when no auth state file present.

Changes merged:
- `AuthenticatedPageTest` + `AuthenticatedDesktopPageTest`: `[Category("Authenticated")]` + `[OneTimeSetUp]` guard
- `deploy.yml`: removed broken "Write auth storage state" step; added `check` preflight job to skip deploy + Playwright entirely on docs-only pushes
- `.githooks/pre-push`: runs unauthenticated Playwright tests on push when `.cs`/`.razor`/`.csproj` files change; skips gracefully if app not running
- `CLAUDE.md`: documents `git config core.hooksPath .githooks` activation

Hook is already active (`git config core.hooksPath .githooks` was run).

### Documentation restructure (PR #69)

Three new documents, architecture.md slimmed down:

| Document | Purpose |
|----------|---------|
| `docs/decisions.md` | Decision register — D001–D014, permanent IDs |
| `docs/technical-debt.md` | Tech debt register — TD1–TD21, Status column, permanent IDs |
| `docs/architecture.md` | System reference — current state, data model, dev setup, Reference section |

**Decision register (D001–D014):**
- D001–D010: core architectural decisions (WASM, MudBlazor, hosting, auth, VSA, etc.)
- D011–D014: Phase 11 showcase decisions (zero Client changes, in-memory persistence, demo watermark, GitHub Pages)

**Tech debt register (TD1–TD21):** 21 entries across infrastructure, CI/CD, auth, observability, security, networking. Each has a Status column (blank = open; `✅ Resolved YYYY-MM — note` when closed). Numbering is permanent — never reuse IDs.

**architecture.md:** Rendering section trimmed from 130 lines to a pointer + prerender patterns. Deep-dive justification moved to new `## Reference` section at bottom. Phase 11 decision record removed (now D011–D014 in decisions.md).

**Claude memory updated:** three-document pattern enforced — decisions → decisions.md, debt → technical-debt.md, reference → architecture.md.

## Phase 11 — GitHub Pages showcase (planned, not started)

Decisions locked in as D011–D014. Implementation plan (from previous session) still applies:

1. Create `TimeTracker.Showcase` project — `Microsoft.NET.Sdk.BlazorWebAssembly`
2. Add project references: `TimeTracker.Client`, `TimeTracker.Contracts`
3. Add NuGet: `MudBlazor`, `Microsoft.AspNetCore.Components.Authorization`
4. Write mock services in `TimeTracker.Showcase/Mock/`:
   - `MockDataStore` (singleton, shared state)
   - `MockTimeEntryService`, `MockTimeEntryQueryService`
   - `MockProjectService`
   - `MockClientService`
5. Write `MockAuthenticationStateProvider`
6. Write `Program.cs` — register mocks instead of HTTP services
7. Write `App.razor` with demo banner above `<Routes />`
8. Add `wwwroot/index.html` with `<base href="/TimeTracker/" />`
9. Add `404.html` SPA routing redirect script
10. Add showcase job to `deploy.yml`
11. Add `TimeTracker.Showcase` to `TimeTracker.sln`
12. Verify build, push branch, open PR

**Risk to verify at step 1:** `TimeTracker.Client` uses `Microsoft.NET.Sdk.BlazorWebAssembly` (not a class library SDK). Referencing one WASM app from another is non-standard — confirm build tooling accepts this before proceeding. Raise before any workaround.

## Node.js 20 deprecation warning
GitHub Actions workflows use Node.js 20 actions. GitHub forcing Node.js 24 from 2026-06-16. Update checkout@v4, setup-dotnet@v4, cache@v4, upload-artifact@v4 before that date.

## Open GitHub issues (legitimate backlog)
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
*Updated at end of session. Replaces previous SESSION.md.*
