# TimeTracker — Architecture

## Overview

TimeTracker is a personal timesheeting application for tracking time entries against projects, managing clients, and year-view reporting across users.

---

## Change log

| Date | Change | PR |
|------|--------|----|
| 2026-06 | Decision: stay on App Service F1 + Cloudflare proxy for custom domain; ACA migration deferred as optional; WASM islands on F1 confirmed feasible | — |
| 2026-06 | Deployed to Azure App Service F1 + Azure SQL; GitHub Actions OIDC push-to-deploy | #43–45 |
| 2026-06 | Security hardening: CSP, HSTS, rate limiting on auth endpoints, 82 tests | #42 |
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
├── TimeTracker.Web         — ASP.NET Core + Blazor SSR + Vertical Slice features + REST API
├── TimeTracker.Shared      — EF Core entities only (class library)
└── TimeTracker.Tests       — xUnit service integration tests (EF InMemory)
```

```
TimeTracker.Web/
  Features/
    Auth/          — Login/Logout pages, ExternalLoginService
    Clients/       — IClientService, ClientService, ClientModels, ClientEndpoints, Pages/
    Projects/      — IProjectService, ProjectService, ProjectModels, ProjectEndpoints, Pages/
    TimeEntries/   — ITimeEntryService, TimeEntryService, TimeEntryModels, TimeEntryEndpoints, Pages/
  Shared/
    IUserContextService, UserContextService
    Components/    — reusable Blazor components
    Layout/        — MainLayout, NavMenu, LoginDisplay
  Data/            — TimeTrackerDataContext, IdentityDataContext
```

### Runtime

- **.NET 10**
- Single process: Blazor Interactive Server serves pages via SignalR; REST API endpoints on the same host
- Deployed to **Azure App Service F1** with **Azure SQL Database** (free offer)
- Custom domain `timetracker.dzk.com.au` served via **Cloudflare proxy** → `*.azurewebsites.net`; TLS terminated at Cloudflare edge
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

- Feature services (`ITimeEntryService`, `IProjectService`, `IAuthService`) injected directly into Blazor pages and minimal API endpoints
- `IUserContextService` extracts the current user's ID from `HttpContext` claims and scopes all queries per user
- REST API endpoints registered via `MapTimeEntryEndpoints()` / `MapProjectEndpoints()` — retained for future Zoho Books integration
- DTOs live in feature-scoped `*Models.cs` files; entities are never exposed to the UI layer

### Authentication

**Cookie-based** with ASP.NET Identity + Google OAuth:
- HTTP-only, Secure, SameSite=Strict cookies; 1-day expiration
- Google OAuth via `Microsoft.AspNetCore.Authentication.Google`; provider-agnostic callback via `SignInManager`
- Allowed emails gated via `Authentication:AllowedEmails` config list
- Login at `/login`, logout at `/logout`
- Local dev DB credentials via **.NET User Secrets** (`DbUser`, `DbPassword`)

### Frontend

**Blazor Interactive Server** (`InteractiveServerRenderMode`) with **MudBlazor** component library. All pages run over a persistent SignalR WebSocket connection.

### Infrastructure (current)

| Concern | Solution | Cost |
|---------|----------|------|
| Hosting | Azure App Service F1 | Free — hard limit, no overage possible |
| Custom domain + SSL | Cloudflare free plan proxy | Free — `timetracker.dzk.com.au` → App Service |
| Database | Azure SQL Database free offer | Free — 32 GB, automated backups, no expiry |
| Auth | Google OAuth 2.0 via ASP.NET Identity | Free |
| CI/CD | GitHub Actions — OIDC push-to-deploy | Free |
| Tests | 82 service integration tests (EF InMemory) | — |

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

## Architecture decisions

### Custom domain — Cloudflare proxy over ACA migration

App Service F1 does not support custom domain binding (requires B1 or higher, ~$13/month). The options considered were:

1. **Upgrade to App Service B1** — recurring cost, no hard spending cap
2. **Migrate to Azure Container Apps (ACA)** — native custom domain + free managed SSL, but ACA Consumption has no hard spending cap; unexpected charges possible
3. **Cloudflare proxy** — `timetracker.dzk.com.au` proxied to `*.azurewebsites.net`; Cloudflare terminates TLS and provides free managed SSL; App Service F1 remains unchanged

**Decision: Cloudflare proxy.** This is a personal single-user timesheeting tool — not a portfolio showcase. The F1 tier's hard free ceiling (no overage possible) is a fundamental constraint. Cloudflare's free plan is also hard-capped. ACA's consumption billing model introduces financial risk with no benefit for this use case.

`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` is set in App Service so that `UseHttpsRedirection()` and `SameSite=Strict` cookies behave correctly behind the Cloudflare TLS proxy (`X-Forwarded-Proto: https`).

### ACA migration — deferred as optional

ACA remains the better long-term architecture (no idle sleep, faster cold starts, more generous free grants, cleaner scale-to-zero). A `Dockerfile` is kept in the repository as the migration artefact. If Microsoft removes the F1 free tier or F1 limits become a problem, the migration is a **workflow change only** — no app code is coupled to App Service. See [roadmap.md](roadmap.md) Optional section for steps.

> ⚠️ ACA Consumption has no hard spending cap. Set a budget alert before enabling.

### Why WASM islands over Blazor Interactive Server (SignalR)

Blazor Interactive Server holds a persistent SignalR WebSocket connection for as long as any page is open. For a timesheeting app with long idle periods between interactions this means:

- Server holds a circuit in memory for hours at a time
- Keepalive pings traverse the connection continuously even when nothing is happening
- If the connection drops the UI freezes, requiring a full page reload

**Decision: WASM islands (Phase 11).** Components that require genuine client-side interactivity (`TimerPage`, `EntrySheet`, `ProjectSheet`, `ClientSheet`) use `@rendermode InteractiveWebAssembly` — .NET runs in the browser for those components. All other pages use static SSR. The server becomes stateless between requests. The timer's `Start` timestamp is written to the database on click; elapsed time is `DateTime.Now - Start` computed client-side — cold starts never affect an in-progress session.

Cookie-based auth (`SameSite=Strict`) is unchanged because WASM static files and API endpoints are served from the same host — no cross-origin split.

---

## Target architecture

```
timetracker.dzk.com.au
        │
   Cloudflare (TLS termination, free proxy)
        │
   Azure App Service F1 (hard free tier — no overage possible)
        ├── /* ────────── Blazor SSR pages + WASM islands (wwwroot)
        └── /api/* ─────── Minimal API endpoints
                │
          Azure SQL Database (free offer — automated backups included)
```

**Properties:**
- $0, hard-capped — no overage possible on any component
- Custom domain + free SSL via Cloudflare
- No persistent connections after Phase 11 (SignalR removed)
- Automated DB backups included in free offer
- No App Service-specific logic in app code — `Dockerfile` in repo enables ACA migration as workflow-only change

### Target solution structure

```
TimeTracker.sln
├── TimeTracker.Web      — ASP.NET Core host: static SSR pages + WASM islands + Minimal API
├── TimeTracker.Showcase — Standalone Blazor WASM project (GitHub Pages portfolio)
├── TimeTracker.Shared   — EF Core entities + DTOs + service interfaces
└── TimeTracker.Tests    — xUnit service integration tests (EF InMemory)
```

---

## Completed phases

#### Phase 4 — Google OAuth ✅

Replace username/password login with Google OAuth.

- Added `Microsoft.AspNetCore.Authentication.Google`
- Provider-agnostic callback via `SignInManager.GetExternalLoginInfoAsync()`
- On callback: find-or-create local user by email, gate against `Authentication:AllowedEmails`
- Removed `IAuthService`, `AuthService`, `RegisterPage`, username/password login
- ASP.NET Identity retained as local user store

#### Phase 5 — Client Management ✅

Add shared `Clients` table with default hourly rate; link projects to clients.

- `Client` entity: `Name` (unique), `DefaultHourlyRate` (nullable, ex GST), `ContactName`, `ContactEmail`, `ContactPhone` (all nullable)
- `Project.ClientId` nullable FK (SET NULL on client delete)
- Clients shared across all users — no per-user scoping
- `IClientService` / `ClientService` / `ClientEndpoints` follow VSA pattern
- 12 new service integration tests (51 total)

#### Phase 6 — MudBlazor UI uplift ✅

Replaced Tailwind + Radzen + QuickGrid with MudBlazor. Mobile-responsive by default.

#### Phase 7 — Security hardening ✅

- `SecurityHeadersMiddleware`: CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `X-XSS-Protection`
- HSTS (365 days)
- Rate limiting on auth endpoints (10 req/min fixed window)
- Managed Identity DB auth — no credentials in connection string
- 82 tests passing

#### Phase 8 — Azure deployment + CI/CD ✅

- Azure SQL Database with free offer applied; Managed Identity auth
- Azure App Service F1; HTTPS-only; system-assigned Managed Identity
- GitHub Actions: OIDC login → publish → deploy on merge to `main`
- Custom domain `timetracker.dzk.com.au` — resolved via Cloudflare proxy in Phase 9

---

## Planned phases

#### Phase 9 — Custom domain via Cloudflare

Resolve the custom domain blocker while remaining on App Service F1.

- `Dockerfile` added to repo root (multi-stage build) — migration artefact for optional ACA move
- Cloudflare proxy configured: `timetracker.dzk.com.au` → `timetracker-zak.azurewebsites.net`
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` set in App Service app settings
- No code changes, no workflow changes

#### Phase 10 — WASM islands (remove SignalR)

Replace Blazor Interactive Server with static SSR + targeted WASM islands. Not a full WASM migration — the server still hosts and renders everything. Only components that genuinely require client-side interactivity use `@rendermode InteractiveWebAssembly`.

- Remove global `InteractiveServerRenderMode` from `Routes.razor` — pages default to static SSR
- Add `Microsoft.AspNetCore.Components.WebAssembly.Server`; create HTTP service implementations for WASM context
- **WASM islands:** `TimerPage`, `EntrySheet`, `ProjectSheet`, `ClientSheet` — `.NET` runs in browser for these components only, no SignalR connection
- **Static SSR:** all other pages — plain server-rendered HTML, no persistent connection
- `TimeEntriesPage` tab/date navigation replaced with URL query params
- Timer elapsed display (`DateTime.Now - Start`) ticks in browser; server only called on Start/Stop button press
- Same origin throughout — `SameSite=Strict` cookies unchanged, Google OAuth unaffected

#### Phase 10 — Playwright UX regression testing

Establish a UI regression baseline against the deployed ACA app before the WASM migration. Golden paths covered: login, start/stop timer, log fixed block, add/edit/delete entries, project and client management, reports. Auth strategy (Google OAuth bypass vs storage state) to be resolved before implementation. See `plan.md` for open questions.

#### Phase 12 — GitHub Pages showcase ⚠️ Needs detailed planning

Add `TimeTracker.Showcase` standalone WASM project to the solution. Shares Razor components with the live app; runs entirely in the browser with mock data — no backend required. Deployed to GitHub Pages via a second job in the existing GitHub Actions workflow.

Key questions to resolve in planning: component sharing strategy, mock data design, auth approach, page scope, and URL.

See `plan.md` Phase 11 for the full list of open questions.

---

## Infrastructure (target)

| Concern | Solution | Cost | Notes |
|---------|----------|------|-------|
| Hosting | Azure App Service F1 | Free — hard cap, no overage | 60 CPU min/day, 1 GB RAM |
| Custom domain + SSL | Cloudflare free plan | Free — hard cap | Proxy to `*.azurewebsites.net` |
| Database | Azure SQL Database free offer | Free — hard cap | 32 GB, automated backups, no expiry |
| Auth | Google OAuth 2.0 via ASP.NET Identity | Free | Cookie-based, SameSite=Strict |
| CI/CD | GitHub Actions — OIDC | Free | Within monthly limits |
| API docs | Scalar UI (dev only) | — | `/scalar/v1` |

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
