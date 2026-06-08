# Session handoff — 2026-06-09

## Current branch
`docs/phase-11-adr` (pushed, PR #67 open)

## Git state
- `main` is clean and up to date
- One open PR: #67 `docs/phase-11-adr` — Phase 11 ADR in architecture.md and roadmap.md

## What was done this session

### Production outage — fixed
Phase 10 (global WASM migration) added `TimeTracker.Client` as a project reference to
`TimeTracker.Web`. This caused two `.runtimeconfig.json` files in the publish output.
Azure App Service couldn't identify which DLL to start and fell back to running
`hostingstart.dll` (the platform placeholder). The app was never running.

Fix applied:
- `az webapp config set --startup-file "dotnet TimeTracker.Web.dll"` applied directly
- `deploy.yml` updated to set startup command before every deploy (PR #64, merged)
- Root cause documented in commit message

### Playwright auth state — NOT fixed
The authenticated Playwright tests (29 of 38) are still failing. Two separate issues:

**Issue 1: `CaptureAuthState` will always fail in CI**
The test navigates to `/api/dev/login` which only exists in Development mode.
It can never pass in CI against production. It's a local-only tool masquerading as a test.

**Issue 2: Auth state captured against wrong domain**
Auth state was captured locally against `https://localhost:7006`.
The CI tests run against `https://timetracker.dzk.com.au`.
ASP.NET Identity cookies have `domain: localhost` — they are never sent to the production domain.
All 29 authenticated tests timeout on `.tt-fab button` (the WASM hydration signal).

**What is NOT broken:**
- The 9 unauthenticated tests all pass
- The production app is running correctly
- WASM loads and hydrates on the live app

**Agreed options (not yet decided):**
1. Fix properly: production auth capture flow + extend cookie lifetime (30 days). ~1 hour.
2. Exclude authenticated tests from CI: only unauthenticated tests in CI, authenticated local-only.
3. Leave it: 9 unauthenticated tests pass in CI, move on to Phase 11.

User was frustrated with time spent on tests vs app. Decision deferred.

## Phase 11 — GitHub Pages showcase (planned, not started)
All decisions locked in. Full ADR in `docs/architecture.md` (Phase 11 section).
PR #67 documents them. Once #67 is merged, Phase 11 implementation can begin.

### Key decisions
- `TimeTracker.Showcase` — new standalone Blazor WASM project
- Zero changes to `TimeTracker.Client` — project reference + mock DI only
- Mock services: `MockTimeEntryService`, `MockProjectService`, `MockClientService`
  — all implement same interfaces, use shared in-memory singleton store
- `MockAuthenticationStateProvider` returns hardcoded "Demo User"
  — satisfies all `[Authorize]` attributes without a login flow
- Persistence: **in-memory only** (Option A) — resets on browser refresh, acceptable for portfolio
- Demo watermark: banner in `TimeTracker.Showcase`'s own `App.razor` — no Client changes
- Deploy: `zkarachiwala.github.io/TimeTracker` via `gh-pages` branch
  — second job in `deploy.yml`, no Azure credentials, fully isolated
- SPA routing: copy `index.html` → `404.html` in CI publish step
- Base href: `/TimeTracker/` set at publish time

### Implementation plan (not started)
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

## Node.js 20 deprecation warning
All GitHub Actions workflows use Node.js 20 actions (checkout@v4, setup-dotnet@v4, etc.).
GitHub is forcing Node.js 24 from 2026-06-16. Actions will need updating soon.
Not urgent until June 16 but worth noting.

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
*This file replaces WORK.md. Updated at the end of each session.*
