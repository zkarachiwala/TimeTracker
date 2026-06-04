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

## Phase 9 — Migrate hosting to Azure Container Apps

**Goal:** Fix the custom domain blocker. Containerise the existing SSR app as-is and deploy to ACA. No code changes beyond adding a Dockerfile.

**Branch:** `feature/aca-migration`

### Steps

1. Add `Dockerfile` to `TimeTracker.Web`
   - Multi-stage build: `sdk` image for publish, `aspnet` runtime image
   - Expose port 8080 (ACA default)

2. Update GitHub Actions `deploy.yml`
   - Replace zip deploy with: build image → push to GHCR → deploy to ACA
   - Reuse existing OIDC auth (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`)

3. Provision ACA in Azure
   - Create Container Apps Environment (Consumption plan)
   - Create Container App pointing at GHCR image
   - Set environment variables: connection string, Google OAuth credentials, `Authentication:AdminEmail`

4. Bind custom domain
   - Add `timetracker.dzk.com.au` in ACA custom domain settings
   - Update DNS — CNAME to ACA verification domain + TXT record
   - Provision free managed certificate (DigiCert, auto-renewed)

5. Validate
   - App loads at `timetracker.dzk.com.au`
   - Google OAuth login works
   - Timer start/stop persists correctly
   - Run `dotnet test` — 82 tests green

**Effort:** ~half day

---

## Phase 10 — Playwright UX regression testing

**Goal:** Establish a UI regression baseline against the deployed ACA app before the WASM migration. Tests run on every PR and protect against regressions through Phases 11 and beyond.

**Why here:** Phase 9 delivers a stable, deployed app at `timetracker.dzk.com.au`. Writing regression tests now captures the correct behaviour as the baseline — then Phase 11 (WASM islands) can be validated against it automatically.

**Branch:** `feature/playwright-tests`

### Golden paths to cover

| Flow | Key assertions |
|------|---------------|
| Unauthenticated redirect | `/` redirects to `/login` |
| Login | Google OAuth completes, lands on timer page |
| Start timer | Running timer card appears, elapsed ticks |
| Stop timer | Entry appears in today's list, duration correct |
| Log a fixed block | Entry appears with correct duration |
| Add time entry manually | Entry saved and visible in time entries page |
| Edit time entry | Changes persisted |
| Delete time entry | Entry removed |
| Add project | Project appears in list and timer dropdown |
| Add client | Client appears in clients list |
| Date navigation | Time entries page steps forward/back correctly |
| Reports page | Loads without error |

### Open questions — to be discussed before implementation

- **Auth strategy** — Google OAuth cannot be automated directly in Playwright; approach TBD
- **Test target** — deployed ACA or local app in CI?
- **Test project location** — new `TimeTracker.Playwright` project or inside `TimeTracker.Tests`?
- **CI trigger** — PR only, or also on push to `main`?
- **Flake tolerance** — timer tests are time-sensitive; cold start handling TBD

### Steps (once open questions resolved)

1. Add `TimeTracker.Playwright` project — `Microsoft.Playwright.NUnit` package
2. Implement auth setup (storage state or dev bypass)
3. Write golden path tests
4. Add `playwright-tests` job to GitHub Actions — runs after `deploy-live`
5. Validate all tests green against ACA

**Effort:** ~1 day (excluding auth strategy decision)

---

## Phase 11 — WASM islands (remove SignalR)

**Goal:** Replace Blazor Interactive Server with static SSR + targeted WASM islands. Eliminates the persistent SignalR WebSocket connection. Server becomes stateless between requests and scales to zero cleanly.

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

## Phase 12 — GitHub Pages showcase ⚠️ NEEDS DETAILED PLANNING

**Goal:** Deploy a mock version of the app as a public portfolio piece on GitHub Pages. Shares UI components with the live app; runs entirely in the browser with no backend.

**Branch:** `feature/showcase`

### What's needed — to be planned in detail

The following questions need answering before implementation begins:

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

## Deployment pipeline (target)

```
git push main
      │
      ├── ci job ────────────── dotnet test (82+ tests)
      │
      ├── deploy-live job ────── Build TimeTracker.Web → Docker → GHCR → ACA
      │                                    (timetracker.dzk.com.au)
      │
      ├── playwright job ──────── Run UX regression tests against ACA
      │       (runs after deploy-live)
      │
      └── deploy-showcase job ─── dotnet publish TimeTracker.Showcase
                                    → static files → gh-pages → GitHub Pages
```
