# TimeTracker — Roadmap

This document describes the phased implementation plan for TimeTracker.

For architecture detail see [architecture.md](architecture.md).

---

## Goals

- Zero-cost hosting on Azure App Service F1 + Azure SQL free offer
- Google OAuth via Gmail account
- Global InteractiveWebAssembly rendering — eliminates SignalR, server stateless between requests (see architecture.md for why islands was rejected)
- Vertical Slice Architecture — feature-organised, no repository layer, interfaces throughout
- Modern mobile-responsive UI (MudBlazor)
- Playwright UX regression test suite
- GitHub Pages portfolio showcase (mock data, standalone WASM)
- Stable REST API layer for future Zoho Books invoice integration

---

## Completed

### Phase 0 — .NET 10 upgrade ✅
- Upgraded all three projects from net7.0 → net10.0
- Replaced Swashbuckle with native ASP.NET Core OpenAPI + Scalar UI (dev only)
- Fixed Swagger unconditionally enabled in production
- Removed `Microsoft.AspNetCore.Authentication` 2.2.0 (bundled since .NET Core 3)

### Phase 1 — Architecture + docs ✅
- Created `docs/architecture.md`
- Created `docs/roadmap.md` (this file)
- Rewrote `README.md`

### Phase 2 — Local SQL Server in Docker ✅
- SQL Server 2022 container for local development
- `IdentityModelSync` migration to align Identity schema with .NET 10
- Fixed high severity vulnerability: `System.Linq.Dynamic.Core` 1.3.7 → 1.7.2

### Phase 3 — Blazor SSR + Vertical Slice Architecture ✅
- Collapsed `TimeTracker.Client` into `TimeTracker.Web`; migrated to Blazor SSR
- Vertical Slice Architecture — feature folders replace Controllers / Services / Repositories
- Cookie auth (HTTP-only, Secure, SameSite=Strict) replaces JWT-in-localStorage
- `JwtBearer`, controller layer, repository layer, `AuthStateProvider`, `Blazored.LocalStorage`, `Blazored.Toast` removed
- DTOs moved from `TimeTracker.Shared` into feature-scoped `*Models.cs` files
- REST API endpoints retained per feature for future Zoho Books integration
- Added `TimeTracker.Tests` — 31 xUnit service integration tests (EF InMemory)
- Added CI workflow — `dotnet test` runs on every push/PR to `main`
- Renamed project `TimeTracker.API` → `TimeTracker.Web` to align with documentation

### Phase 4 — External OAuth ✅
- Added `Microsoft.AspNetCore.Authentication.Google`
- Provider-agnostic callback via `SignInManager.GetExternalLoginInfoAsync()`
- On callback: find-or-create local user by email, check against `Authentication:AllowedEmails` config list
- Removed `IAuthService`, `AuthService`, `RegisterPage`, username/password login
- ASP.NET Identity retained as local user store
- See `docs/google-oauth-setup.md` for Google Cloud Console setup steps

### Phase 5 — Client Management ✅
- `Client` entity: `Name` (unique), `DefaultHourlyRate` (nullable, ex GST), contact fields
- `Project.ClientId` nullable FK — SET NULL on client delete; service layer blocks delete when active projects exist
- Clients shared across all users; Admin-only CRUD
- 12 new service integration tests (51 total)

### Phase 6 — MudBlazor UI uplift ✅
- `MudLayout` + responsive `MudNavMenu` drawer
- Bottom sheet components (EntrySheet, ProjectSheet, ClientSheet)
- `MudChart` stacked bar (non-billable/invoiced/uninvoiced) replaces Radzen
- Tailwind CSS, Radzen, QuickGrid removed
- `ProjectDetails` table merged into `Projects`; `InvoiceReference` + `InvoicedAt` added to `TimeEntry`
- Dev seed/clear endpoints (dev only)

### Phase 7 — Security hardening ✅
- `SecurityHeadersMiddleware`: CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `X-XSS-Protection`
- HSTS (365 days); rate limiting on auth endpoints (10 req/min)
- Managed Identity — App Service authenticates to Azure SQL with no stored credentials
- Least privilege DB user (`db_datareader` + `db_datawriter` only)
- 83 tests passing

### Phase 8 — Azure deployment + CI/CD ✅
- Azure SQL Database — free offer (32 GB, automated backups, Managed Identity auth)
- Azure App Service F1 — deployed and functional
- GitHub Actions — OIDC push-to-deploy on merge to `main`
- EF Core migrations applied automatically at startup
- See `docs/azure-deployment.md` for one-time Azure resource setup steps

### Phase 9 — Playwright UX regression testing ✅
- `TimeTracker.Playwright` NUnit project with `Microsoft.Playwright.NUnit`
- Auth strategy: storage state replay — `playwright/.auth/user.json` encoded as `PLAYWRIGHT_AUTH_STATE_B64` GitHub secret
- Test target: deployed App Service (`timetracker.dzk.com.au`) post-deploy
- Coverage: unauthenticated redirects, login page, timer, time entries, projects, clients, reports, navigation
- Playwright job in `deploy.yml` runs after `deploy` job succeeds on `main`

### Phase 10 — Global InteractiveWebAssembly migration ✅
Replaced Blazor Interactive Server (SignalR) with global InteractiveWebAssembly. Server is now stateless between requests.

- `TimeTracker.Client` (Blazor WASM) — all routed pages, layouts, HTTP service implementations
- `TimeTracker.Contracts` — shared DTOs and service interfaces (Web + Client + Tests)
- `CookieAuthenticationStateProvider` — WASM auth state via `/api/auth/user`
- `CookieCredentialHandler` — forwards auth cookie with every WASM HTTP request
- WASM islands (per-component render mode) evaluated and rejected — MudBlazor `MudDrawer` incompatible with SSR layouts; see `architecture.md` for full decision record

### Phase 11 — GitHub Pages showcase ✅
Standalone WASM showcase with mock data hosted at [zkarachiwala.github.io/TimeTracker](https://zkarachiwala.github.io/TimeTracker/).

- `#if SHOWCASE` compile flag swaps production services for in-memory mock implementations
- Showcase assets isolated to `wwwroot-showcase/` — invisible to the normal SDK build, no fingerprint churn in dev
- `<base href="/TimeTracker/" />` subpath hosting — all navigation paths converted to base-href-agnostic relative paths
- SPA routing via `404.html` redirect script (`pathSegmentsToKeep=1`)
- Deployed automatically by a second job in `deploy.yml` whenever app code changes land on `main`

---

## Future

### Zoho Books integration
TimeTracker will eventually integrate with Zoho Books to partially automate invoice generation based on tracked time entries. The REST API layer is retained specifically to support this. Direction TBD — push from TimeTracker or pull from Zoho.

---

## Phase dependency order

```
0 ✅ → 1 ✅ → 2 ✅ → 3 ✅ → 4 ✅ → 5 ✅ → 6 ✅ → 7 ✅ → 8 ✅ → 9 ✅ → 10 ✅ → 11 ✅ → Zoho
```

## Infrastructure summary

| Service | Plan | Free grants | Overage behaviour |
|---------|------|-------------|-------------------|
| Azure App Service | F1 | 60 CPU min/day, 1 GB RAM | Throttled — no charge possible |
| Azure SQL Database | Free offer | 32 GB data, automated backups (permanent) | Auto-pauses — no charge |
| Google OAuth | — | Unlimited personal use | — |
| GitHub Actions | Free | 2,000 min/month | Queued — no charge |
