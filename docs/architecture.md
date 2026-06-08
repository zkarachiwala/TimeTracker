# TimeTracker — Architecture

## Overview

TimeTracker is a personal timesheeting application for tracking time entries against projects, managing clients, and year-view reporting.

---

## Change log

| Date | Change | PR/Branch |
|------|--------|-----------|
| 2026-06 | **Global InteractiveWebAssembly** — abandoned SSR+WASM islands hybrid; MudBlazor #9743 prevents interactive layouts in SSR | `feature/wasm-islands` |
| 2026-06 | Renamed `TimeTracker.Wasm` → `TimeTracker.Client` (Microsoft standard .Client naming) | `feature/wasm-islands` |
| 2026-06 | Added `TimeTracker.Contracts` — shared DTOs; `CookieAuthenticationStateProvider`; `/api/auth/user` endpoint; `ReportsCalculations` static class; 92 unit tests | `feature/wasm-islands` |
| 2026-06 | Added `TimeTracker.Playwright` — E2E tests; Cloudflare custom domain `timetracker.dzk.com.au` | #43–56 |
| 2026-06 | Deployed to Azure App Service F1 + Azure SQL; GitHub Actions OIDC push-to-deploy | #43–45 |
| 2026-06 | Security hardening: CSP, HSTS, rate limiting, 83 tests | #42 |
| 2026-05 | MudBlazor UI uplift; replaced Tailwind + Radzen + QuickGrid | #38 |
| 2026-05 | Added `Clients` table; client CRUD feature; project–client FK; 12 new tests (51 total) | #29 |
| 2026-05 | Google OAuth; removed username/password login | #28 |
| 2026-05 | Renamed `TimeTracker.API` → `TimeTracker.Web` to align with documentation | #26 |
| 2026-05 | Added `TimeTracker.Tests` — 31 service integration tests (EF InMemory); CI runs `dotnet test` on every PR | #25 |
| 2026-05 | Migrated to Blazor SSR + Vertical Slice Architecture; removed `TimeTracker.Client` | #25 |
| 2026-05 | Upgraded solution from .NET 7 → .NET 10 | #20 |
| 2026-05 | Replaced Swashbuckle with native ASP.NET Core OpenAPI + Scalar UI (dev only) | #20 |

---

## Current State

### Solution structure

```
TimeTracker.sln
├── TimeTracker.Web         — ASP.NET Core host: App.razor shell, API endpoints, EF Core, static assets
├── TimeTracker.Client      — Blazor WASM client: all routed pages, layouts, HTTP services
├── TimeTracker.Contracts   — Shared DTOs and interfaces (referenced by both Web and Client)
├── TimeTracker.Shared      — EF Core entities only (referenced by Web only)
├── TimeTracker.Tests       — xUnit unit tests (EF InMemory, no running DB required)
└── TimeTracker.Playwright  — End-to-end Playwright browser tests
```

```
TimeTracker.Web/
  Features/
    Auth/          — Login/Logout pages, ExternalLoginService, /api/auth/user endpoint
    Clients/       — IClientService, ClientService, ClientModels, ClientEndpoints
    Projects/      — IProjectService, ProjectService, ProjectModels, ProjectEndpoints
    TimeEntries/   — ITimeEntryService, TimeEntryService, TimeEntryModels, TimeEntryEndpoints
    Reports/       — ReportsEndpoints (no SSR page — page lives in Client)
  Data/            — TimeTrackerDataContext, IdentityDataContext

TimeTracker.Client/
  Routes.razor     — WASM router; must live here for WASM to boot it
  Features/
    Auth/          — CookieAuthenticationStateProvider
    Clients/       — Pages/, Components/, HttpClientService
    Projects/      — Pages/, Components/, HttpProjectService
    TimeEntries/   — Pages/, Components/, HttpTimeEntryService
    Timer/         — Pages/
    Reports/       — Pages/
  Shared/
    Layout/        — MainLayout, NavMenu, BottomNav, LoginLayout
    Components/    — RedirectToLogin, shared UI
    Theme/         — DzkTheme
```

### Runtime

- **.NET 10**
- **Global InteractiveWebAssembly** rendering — `App.razor` renders `<Routes @rendermode="InteractiveWebAssembly" />`. The entire routed app runs as WASM in the browser. No SignalR.
- REST API endpoints served from the same ASP.NET Core host
- Deployed to **Azure App Service F1** with **Azure SQL Database** (free offer)
- Custom domain `timetracker.dzk.com.au` via Cloudflare proxy (Cloudflare terminates TLS)
- Runs at `https://localhost:7006` (dev). API docs at `/scalar/v1` (dev only).

### Data layer

Two EF Core `DbContext`s, both targeting **SQL Server** (`TimeTrackerDb`):

| Context | Schema | Tables |
|---------|--------|--------|
| `TimeTrackerDataContext` | `app` | `Clients`, `TimeEntries`, `Projects`, `ProjectDetails`, `ProjectUsers` |
| `IdentityDataContext` | `id` | ASP.NET Identity tables |

- `Client` is shared across all users — no `UserId` scoping. `Name` has a unique index. `DefaultHourlyRate` is nullable (ex GST). Supports soft-delete (`IsDeleted`) for recoverability and archiving (`IsArchived`) to hide inactive clients from dropdowns without deleting them.
- `Project` uses soft-delete (`SoftDeleteableEntity`). `ClientId` is a nullable FK — deleting a client with active projects is blocked at the service layer; the DB cascades to `SET NULL` if bypassed.
- `TimeEntry` stores `UserId` (string) rather than a navigation property to avoid cascade delete issues
- **Mapster** handles entity ↔ DTO mapping, configured via per-feature `IRegister` classes scanned at startup

### Architecture

**Vertical Slice Architecture** — no controllers, no repository layer.

- Feature services (`ITimeEntryService`, `IProjectService`, `IAuthService`) injected directly into minimal API endpoints on the server
- In `TimeTracker.Client`, HTTP service implementations (`HttpTimeEntryService`, etc.) call the REST API; these are what the WASM pages inject
- `IUserContextService` extracts the current user's ID from `HttpContext` claims and scopes all queries per user (server-side only)
- REST API endpoints registered via `MapTimeEntryEndpoints()` / `MapProjectEndpoints()` — retained for future Zoho Books integration
- DTOs live in `TimeTracker.Contracts/` — shared between Web (Mapster mapping source) and Client (HTTP deserialisation target)

### Authentication

**Cookie-based** with ASP.NET Identity + Google OAuth:
- HTTP-only, Secure, SameSite=Strict cookies; 1-day expiration
- `CookieCredentialHandler` in Client sends `BrowserRequestCredentials.Include` with every HTTP request so the auth cookie is forwarded
- `CookieAuthenticationStateProvider` calls `/api/auth/user` on first load to hydrate WASM auth state; result cached per circuit
- On 401 mid-session, pages call `Nav.NavigateTo("/login", forceLoad: true)` to force full reload and reset WASM state
- Google OAuth via `Microsoft.AspNetCore.Authentication.Google`; provider-agnostic callback via `SignInManager`
- OAuth challenge links use `data-enhance-nav="false"` to force full-page navigation (Blazor enhanced nav would turn it into a fetch, blocked by CSP)
- Allowed emails gated via `Authentication:AllowedEmails` config list
- Login at `/login`, logout at `/auth/logout`
- Local dev DB credentials via **.NET User Secrets** (`DbUser`, `DbPassword`)

### Rendering

**Global InteractiveWebAssembly** with **MudBlazor** component library.

- `App.razor` (server) is a non-interactive HTML shell only; it renders `<Routes @rendermode="InteractiveWebAssembly" />`
- `Routes.razor` and all layout/page components live in `TimeTracker.Client` — the WASM bundle does not include `TimeTracker.Web.dll`
- MudBlazor providers (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`) live once in `MainLayout.razor` — never on individual pages
- Never add `@rendermode` to individual pages — render mode is inherited globally
- `IWebAssemblyHostEnvironment` (not `IWebHostEnvironment`) is used in Client for environment checks

#### SSR prerender → WASM hydration patterns

Global WASM apps serve an SSR-prerendered HTML page first, then WASM boots and hydrates. Two patterns are in use to handle the gap:

**1. Disabling controls during prerender (`RendererInfo.IsInteractive`)**

Controls in `MainLayout` (e.g. the hamburger button) appear in the SSR HTML before WASM has attached event handlers. Adding `Disabled="@(!RendererInfo.IsInteractive)"` disables them during prerender and enables them once WASM is interactive. `RendererInfo.IsInteractive` is a .NET 9+ `ComponentBase` property: `false` during SSR prerender, `true` after WASM hydration.

Playwright's `ClickAsync` actionability check (waits for Enabled) automatically waits for WASM hydration — no custom wait logic needed. This is the Microsoft-documented approach; no test scaffolding in production code.

Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0

**2. Resetting layout state on navigation (`NavigationManager.LocationChanged`)**

`MainLayout` is a persistent WASM component — it is NOT destroyed on client-side navigation. State fields like `drawerOpen` retain their values across route changes. `OnLocationChanged` is the correct place to reset transient UI state:

```csharp
private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
{
    drawerOpen = false;
    InvokeAsync(StateHasChanged);
}
```

`LocationChanged` fires on every navigation: browser-intercepted link clicks and programmatic `NavigateTo` calls. Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0

#### Why global WASM — not SSR + WASM islands

This was an explicit, researched decision. Do not revisit without re-reading this section.

The original Phase 10 plan targeted true WASM islands: SSR router, only interactive components in `TimeTracker.Client`. This was abandoned because **MudBlazor is architecturally incompatible with hybrid/islands rendering** in the following concrete ways:

1. **`MudDrawer` is broken in SSR layouts** — [MudBlazor #9743](https://github.com/MudBlazor/MudBlazor/issues/9743) documents that `MudDrawer` does not work on SSR pages in mobile layout. The drawer toggle state is managed through Blazor's component tree; when the layout is SSR, clicking the hamburger does nothing. Closed as **not planned** by MudBlazor maintainers.

2. **`MudNavMenu` cannot be toggled on SSR pages** — [MudBlazor/Templates #478](https://github.com/MudBlazor/Templates/issues/478) confirms that on SSR-rendered pages the nav drawer is non-functional on mobile. This is a showstopper for a mobile-first app.

3. **MudBlazor providers require an interactive render context** — `MudThemeProvider`, `MudDialogProvider`, `MudSnackbarProvider`, and `MudPopoverProvider` cannot function in static SSR. The `MainLayout` cannot be fully SSR-rendered. Confirmed in [MudBlazor Discussion #7430](https://github.com/MudBlazor/MudBlazor/discussions/7430).

4. **Theme flash** — In a hybrid model, `MudThemeProvider` applies theme colours at SSR render time, then reapplies when WASM boots, causing a visible colour flash. No MudBlazor fix exists; the workaround requires a custom JavaScript shim reading from `localStorage` before render. Documented in [MudBlazor #10946](https://github.com/MudBlazor/MudBlazor/issues/10946).

5. **These are MudBlazor limitations, not .NET limitations** — .NET 10 supports hybrid rendering perfectly. The constraint is MudBlazor's architecture. Switching UI libraries would re-open the islands option, but that is a far larger refactor than the current approach.

**The workaround for islands** (putting MudBlazor providers on every individual page and using `@rendermode InteractiveAuto` on the drawer) was evaluated and rejected: it requires JavaScript interop hacks for drawer state, produces an inconsistent UX, and creates ongoing maintenance burden for every new page.

**Conclusion:** Global `InteractiveWebAssembly` is the correct and stable choice for a MudBlazor app. All routed pages and layouts must live in `TimeTracker.Client`. The server project (`TimeTracker.Web`) is a shell: API endpoints, EF Core, and the `App.razor` HTML wrapper only.

#### Rate limiting — split policies for auth vs auth-status

Two named rate limit policies, both driven from `RateLimiting` config in `appsettings.json`:

| Policy | Endpoints | Production limit | Dev override |
|--------|-----------|-----------------|--------------|
| `"auth"` | `/auth/challenge`, `/auth/callback` | 10 req/min | — (same) |
| `"auth-status"` | `/api/auth/user` | 10 req/min | 200 req/min |

**Why the split:** OWASP rate limiting guidance targets credential submission endpoints — login attempts and OAuth initiation — to prevent brute force. `/api/auth/user` is a read-only cookie validation (no credentials accepted); applying the same 10/min limit had no security benefit and caused the Playwright test suite to fail mid-run with 429s. Reference: https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html

**Why config-driven:** Microsoft docs recommend driving rate limit options from `Configuration` so limits can differ per environment without code changes. Code falls back to 10/min if the config key is absent — safe for existing deployments. Reference: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0

**Dev override location:** `appsettings.Development.json` → `RateLimiting:AuthStatus:PermitLimit = 200`. Production `appsettings.json` keeps both at 10.

### Infrastructure

| Concern | Solution | Cost |
|---------|----------|------|
| Hosting | Azure App Service F1 | Free — hard limit, no overage possible |
| Database | Azure SQL Database free offer | Free — 32 GB, 7-day automated backups, no expiry |
| Auth | Google OAuth 2.0 via ASP.NET Identity | Free |
| CI/CD | GitHub Actions — OIDC push-to-deploy | Free |
| Tests | 83 service integration tests (EF InMemory) | — |

---

## Data Model

### `app` schema

```mermaid
erDiagram
    Client {
        int Id PK
        string Name "unique"
        bool IsArchived
        bool IsDeleted
        datetime DateDeleted "nullable"
        decimal DefaultHourlyRate "nullable, ex GST"
        string ContactName "nullable"
        string ContactEmail "nullable"
        string ContactPhone "nullable"
        datetime DateCreated
        datetime DateUpdated "nullable"
    }
    TimeEntry {
        int Id PK
        int ProjectId FK
        datetime Start
        datetime End "nullable"
        string UserId "ASP.NET Identity user Id"
        datetime DateCreated
        datetime DateUpdated "nullable"
    }
    Project {
        int Id PK
        string Name
        int ClientId FK "nullable"
        decimal HourlyRate "nullable, ex GST"
        bool IsDeleted
        datetime DateDeleted "nullable"
        datetime DateCreated
        datetime DateUpdated "nullable"
    }
    ProjectDetails {
        int Id PK
        int ProjectId FK
        string Description "nullable"
        datetime StartDate "nullable"
        datetime EndDate "nullable"
    }
    ProjectUser {
        int Id PK
        int ProjectId FK
        string UserId "ASP.NET Identity user Id"
    }

    Client ||--o{ Project : "ClientId (SET NULL on delete)"
    TimeEntry }o--|| Project : "ProjectId (nullable)"
    Project ||--o| ProjectDetails : "ProjectId"
    Project ||--o{ ProjectUser : "ProjectId"
```

### `id` schema (ASP.NET Identity)

```mermaid
erDiagram
    AspNetUsers {
        string Id PK
        string UserName
        string NormalizedUserName
        string Email
        string NormalizedEmail
        string PasswordHash
        string SecurityStamp
    }
    AspNetRoles {
        string Id PK
        string Name
        string NormalizedName
    }
    AspNetUserRoles {
        string UserId FK
        string RoleId FK
    }

    AspNetUsers ||--o{ AspNetUserRoles : "UserId"
    AspNetRoles ||--o{ AspNetUserRoles : "RoleId"
```

> `TimeEntry.UserId` and `ProjectUser.UserId` reference `AspNetUsers.Id` by convention (string foreign key). No FK constraint is defined to avoid cascade delete issues.

---

## Planned phases

#### Current — Global WASM migration (`feature/wasm-islands`)

In progress. Migrating all remaining SSR pages to `TimeTracker.Client` so the WASM router can reach them. Full verification (build → unit tests → Playwright) required before merge.

#### Next — GitHub Pages showcase ⚠️ Needs planning session

Add a standalone WASM showcase project. Shares Razor components with the live app; runs in the browser with mock data. Deployed to GitHub Pages.

---

## Development setup

### Prerequisites
- .NET 10 SDK
- Docker Desktop (Windows) — for local SQL Server

### SQL Server (Docker)
```bash
docker run \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1435:1433 \
  --name timetracker-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

> Port 1435 is used because 1433 and 1434 are reserved by the Windows SQL Server instance.
> Connect via SSMS using `127.0.0.1,1435`, SQL auth (sa), with `Encrypt=false;TrustServerCertificate=true` in Additional Connection Parameters.

### User secrets
```bash
cd TimeTracker.Web
dotnet user-secrets set "DbUser" "sa"
dotnet user-secrets set "DbPassword" "YourStrong@Passw0rd"
```

### Run
```bash
cd TimeTracker.Web
dotnet run
# App: https://localhost:7006
# API docs (dev): https://localhost:7006/scalar/v1
```

### EF Core migrations
```bash
cd TimeTracker.Web
dotnet ef migrations add <Name> --context TimeTrackerDataContext
dotnet ef migrations add <Name> --context IdentityDataContext
dotnet ef database update --context TimeTrackerDataContext
dotnet ef database update --context IdentityDataContext
```
