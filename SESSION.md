# Session handoff — 2026-06-10

## Current branch
`feat/phase-11-showcase` — PR #72 open, awaiting CI + merge

## Git state
- Open PR: #72 — feat: Phase 11 — GitHub Pages showcase
- No uncommitted changes
- Branch is fully pushed

## What was done this session

### Phase 11 — GitHub Pages showcase (PR #72)

Full implementation of the GitHub Pages demo. Key architectural choices and what was fixed:

**Approach:** `#if SHOWCASE` compile flag in `TimeTracker.Client/Program.cs` swaps HTTP services for mock implementations. No changes to any existing page, layout, or component.

**Mock layer added (`TimeTracker.Client/Mock/`):**
- `MockDataStore` — singleton, seeded with 2 clients, 4 projects, ~50 time entries
- `MockAuthenticationStateProvider` — returns `demo@example.com` with Admin role
- `MockTimeEntryService`, `MockProjectService`, `MockClientService`

**Showcase hosting files isolated to `wwwroot-showcase/` (D015):**
- `index.html`, `404.html`, `showcase-app.css` moved out of `wwwroot/`
- `TimeTracker.Client.csproj` has conditional `ItemGroup` — files only mapped into publish output when `-p:Showcase=true`
- Prevents asset fingerprint churn in local dev server from showcase file changes
- CI command: `-p:Showcase=true -p:DefineConstants=SHOWCASE`

**Root components fix:** Standalone WASM (GitHub Pages) requires explicit `builder.RootComponents.Add<>()` — hosted mode doesn't. Added inside `#if SHOWCASE` block.

**Playwright error monitoring fixed (all three base classes):**
- Added `Page.RequestFailed` handler (filters `.pdb` URLs)
- Console listener now filters: `"Failed to load resource"`, `"Failed to load module script"`, `.pdb`-related noise
- All stale-asset false failures eliminated

**Pre-push hook improved:**
- Staleness detection: compares `blazor.boot.json` from local build vs running server
- If stale: blocks with `"restart dotnet run, then re-push"` instead of cryptic test failures

**Decisions and tech debt recorded:**
- D015: showcase static assets isolated to `wwwroot-showcase/`
- TD22: pre-push hook relies on manually-maintained local dev server (no staging environment)

## Before merging PR #72

**Required:** Enable GitHub Pages in repo settings:
**Settings → Pages → Source: GitHub Actions**

## Node.js 20 deprecation warning
GitHub Actions workflows use Node.js 20 actions. GitHub forcing Node.js 24 from 2026-06-16. Update checkout@v4, setup-dotnet@v4, cache@v4, upload-artifact@v4, upload-pages-artifact@v3, deploy-pages@v4 before that date.

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
