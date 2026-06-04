# TimeTracker — Active Plan

## Completed phases

| Phase | Description | PR |
|-------|-------------|-----|
| 4 | Google OAuth — replaced username/password login | #28 |
| 5 | Client management — Clients table, project–client FK | #29 |
| 6 | MudBlazor UI uplift — replaced Tailwind + Radzen + QuickGrid | #38 |
| 7 | Security hardening — CSP, HSTS, rate limiting, Managed Identity | #42 |
| 8 | Azure deployment + CI/CD — App Service F1, Azure SQL, GitHub Actions OIDC | #43–45 |

---

## Phase 9 — Playwright UX regression testing ✅ IMPLEMENTED

**Goal:** Establish a UI regression baseline against the deployed app before the WASM migration.

**Auth strategy:** Storage state replay — generate `playwright/.auth/user.json` locally, encode as base64, store as `PLAYWRIGHT_AUTH_STATE_B64` GitHub secret.

**Test target:** Deployed App Service (`timetracker-zak.azurewebsites.net`), post-deploy.

**CI trigger:** `playwright` job in `deploy.yml` runs after `deploy` job succeeds on `main`.

### What was built

- `TimeTracker.Playwright/` — NUnit project with `Microsoft.Playwright.NUnit` 1.52
- `AuthenticatedPageTest.cs` — base class loading storage state + mobile viewport
- `Tests/AuthTests.cs` — unauthenticated redirect, login page Google button
- `Tests/TimerTests.cs` — page load, start/stop timer, log fixed block, FAB
- `Tests/TimeEntriesTests.cs` — page load, filter tabs, summary card, date step, month/project views
- `Tests/ProjectsTests.cs` — page load, heading, active count chip, add sheet opens
- `Tests/ClientsTests.cs` — page load, heading, active count chip, add sheet opens
- `Tests/ReportsTests.cs` — page load, no error, content renders
- `deploy.yml` — `playwright` job added after `deploy`

### ⚠️ One manual step remaining — auth state setup

Before the CI job will pass, generate the storage state file and add it as a GitHub secret:

```bash
# 1. Install browsers (once)
cd TimeTracker.Playwright
dotnet build
pwsh bin/Release/net10.0/playwright.ps1 install chromium

# 2. Launch codegen against the live app and log in with Google
pwsh bin/Release/net10.0/playwright.ps1 codegen \
  --save-storage=playwright/.auth/user.json \
  https://timetracker-zak.azurewebsites.net

# 3. Encode and copy to clipboard
base64 -w 0 playwright/.auth/user.json | xclip -selection clipboard

# 4. Add to GitHub → Settings → Secrets → New repository secret
#    Name: PLAYWRIGHT_AUTH_STATE_B64
#    Value: (paste)
```

---

## Phase 10 — WASM islands (remove SignalR)

**Goal:** Replace Blazor Interactive Server with static SSR + targeted WASM islands. Eliminates the persistent SignalR WebSocket connection. Server becomes stateless between requests.

**Branch:** `feature/wasm-islands`

### Approach

Most pages become plain static SSR (no render mode, no connection held). Only the components that genuinely need client-side interactivity use `@rendermode InteractiveWebAssembly`. The server still hosts everything — this is not a hosted WASM model, just selective WASM islands embedded in SSR pages.

### Steps

1. **Configure WASM hosting in `TimeTracker.Web`**
   - Add `Microsoft.AspNetCore.Components.WebAssembly.Server` package
   - Register WASM services in `Program.cs`

2. **Create HTTP service implementations**
   - `HttpTimeEntryService : ITimeEntryService` — calls existing `/api/timeentries/*` endpoints
   - `HttpProjectService : IProjectService` — calls existing `/api/projects/*` endpoints
   - `HttpClientService : IClientService` — calls existing `/api/clients/*` endpoints
   - Register HTTP versions for WASM context; EF Core versions for server context

3. **Remove global render mode from `Routes.razor`**
   - All pages default to static SSR

4. **Convert pages to static SSR**
   - `ReportsPage` — no changes needed, just remove `@rendermode`
   - `ProjectsPage`, `ProjectDetailPage` — remove `@rendermode`
   - `ClientsPage`, `ClientDetailPage` — remove `@rendermode`
   - `LoginPage` — convert to standard form POST
   - `TimeEntriesPage` — replace tab/date/filter client state with URL query params

5. **Promote interactive components to WASM islands**
   - `TimerPage` → `@rendermode InteractiveWebAssembly`
   - `EntrySheet` → `@rendermode InteractiveWebAssembly`
   - `ProjectSheet` → `@rendermode InteractiveWebAssembly`
   - `ClientSheet` → `@rendermode InteractiveWebAssembly`

6. **Fix `MainLayout` / `BottomNav`**
   - Remove `InvokeAsync(StateHasChanged)` calls; replace with CSS/JS where needed

7. **Validate**
   - All pages load as static SSR (verify no SignalR connection in browser devtools)
   - Timer start/stop works; elapsed display ticks in browser
   - Sheets open, save, and close correctly
   - Google OAuth still works (same origin — no cookie issues)
   - Run `dotnet test` — all green

**Effort:** ~1 day

---

## Phase 11 — GitHub Pages showcase ⚠️ NEEDS DETAILED PLANNING

**Goal:** Deploy a mock version of the app as a public portfolio piece on GitHub Pages. Shares UI components with the live app; runs entirely in the browser with no backend.

**Branch:** `feature/showcase`

### What's needed — to be planned in detail

- **Project structure** — standalone WASM project added to the solution (`TimeTracker.Showcase`) or a publish profile on `TimeTracker.Web`?
- **Component sharing** — do the Razor components (pages, sheets, layout) live in a Razor Class Library referenced by both, or are they duplicated?
- **Mock data design** — what sample data tells a good story? Realistic client names, project names, time entries across multiple days/weeks?
- **Mock service implementations** — in-memory only, or persist to `localStorage` so demo state survives page refreshes?
- **Auth** — skip entirely (app loads pre-authenticated as a demo user), or a mock "Login with Google" button that immediately succeeds?
- **Navigation** — full app with all pages, or a curated subset (timer + time entries + reports)?
- **GitHub Actions** — additional job in `deploy.yml`: `dotnet publish TimeTracker.Showcase → push to gh-pages branch`
- **URL** — `zkarachiwala.github.io/TimeTracker` or a subdomain of `dzk.com.au`?
- **Branding** — banner or footer making clear it's a demo/portfolio piece

**Effort:** To be estimated after planning session

---

## Deployment pipeline (current)

```
git push main
      │
      ├── ci job ──────────────── dotnet test
      │
      ├── deploy-live job ──────── dotnet publish → Azure App Service F1
      │                            (timetracker-zak.azurewebsites.net)
      │
      ├── playwright job ──────── Run UX regression tests against deployed app
      │       (runs after deploy-live, Phase 9)
      │
      └── deploy-showcase job ─── dotnet publish TimeTracker.Showcase
                                    → static files → gh-pages → GitHub Pages
                                    (Phase 11)
```
