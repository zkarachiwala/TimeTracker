# TimeTracker — Roadmap

This document describes the phased implementation plan for evolving TimeTracker into a production-ready, free-hosted personal timesheeting solution.

For architecture detail see [architecture.md](architecture.md).

---

## Goals

- Zero-cost hosting on Azure Container Apps (Consumption) + Azure SQL free offer
- Custom domain (`timetracker.dzk.com.au`) with free managed SSL
- Google OAuth via Gmail account
- Blazor SSR with targeted WASM islands — removes SignalR, server scales to zero cleanly
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
- 82 tests passing

### Phase 8 — Azure deployment + CI/CD ✅
- Azure SQL Database — free offer (32 GB, automated backups, Managed Identity auth)
- Azure App Service F1 — deployed and functional; custom domain blocked (resolved in Phase 9)
- GitHub Actions — OIDC push-to-deploy on merge to `main`
- EF Core migrations applied automatically at startup
- See `docs/azure-deployment.md` for one-time Azure resource setup steps

---

## Upcoming

### Phase 9 — Migrate hosting to Azure Container Apps
Fix the custom domain blocker. Containerise the existing SSR app as-is and deploy to ACA. No code changes beyond adding a Dockerfile.

- `Dockerfile` added to `TimeTracker.Web` (multi-stage build)
- GitHub Actions updated: build image → push to GHCR → deploy to ACA
- Custom domain `timetracker.dzk.com.au` bound in ACA with free managed certificate

### Phase 10 — Playwright UX regression testing
Establish a UI regression baseline against the deployed ACA app before the WASM refactor.

- Golden paths: login, start/stop timer, log fixed block, add/edit/delete entries, projects, clients, reports
- Auth strategy TBD (to be discussed before implementation)
- Playwright job added to GitHub Actions — runs after `deploy-live`

### Phase 11 — WASM islands (remove SignalR)
Replace Blazor Interactive Server with static SSR + targeted WASM islands. Server becomes stateless between requests.

- Most pages: static SSR — no persistent connection
- `TimerPage`, `EntrySheet`, `ProjectSheet`, `ClientSheet`: `@rendermode InteractiveWebAssembly`
- HTTP service implementations for WASM context; EF Core implementations for server context
- `TimeEntriesPage` tab/date navigation replaced with URL query params

### Phase 12 — GitHub Pages showcase ⚠️ Needs planning session
Add `TimeTracker.Showcase` standalone WASM project. Shares components with the live app; runs in the browser with mock data. Deployed to GitHub Pages via a second job in the GitHub Actions workflow.

---

## Future

### Zoho Books integration
TimeTracker will eventually integrate with Zoho Books to partially automate invoice generation based on tracked time entries. The REST API layer is retained specifically to support this. Direction TBD — push from TimeTracker or pull from Zoho.

---

## Phase dependency order

```
0 ✅ → 1 ✅ → 2 ✅ → 3 ✅ → 4 ✅ → 5 ✅ → 6 ✅ → 7 ✅ → 8 ✅ → 9 → 10 → 11 → 12 → Zoho
```

## Infrastructure summary (target)

| Service | Plan | Free grants | Overage behaviour |
|---------|------|-------------|-------------------|
| Azure Container Apps | Consumption | 180K vCPU-sec, 360K GiB-sec, 2M req/month (permanent) | Metered — negligible at personal-app traffic |
| Azure SQL Database | Free offer | 32 GB data, automated backups (permanent) | Throttled — no charge |
| Google OAuth | — | Unlimited personal use | — |
| GitHub Actions | Free | 2,000 min/month | Queued — no charge |
| GitHub Container Registry | Free (current) | No published limit | 30-day notice before billing |
