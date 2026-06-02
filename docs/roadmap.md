# TimeTracker — Roadmap

This document describes the phased implementation plan for evolving TimeTracker into a production-ready, free-hosted personal timesheeting solution.

For architecture detail see [architecture.md](architecture.md).

---

## Goals

- Zero-cost 24x7 hosting on Azure (App Service F1 + Azure SQL free offer)
- Google OAuth via Gmail account
- Blazor SSR — single project, no separate WASM client
- Vertical Slice Architecture — feature-organised, no repository layer, interfaces throughout
- Modern mobile-responsive UI (MudBlazor)
- Stable REST API layer for future Zoho Books invoice integration
- Security best practices — Managed Identity, HTTP-only cookies, no secrets in source control

---

## Completed

### Phase 0 — .NET 10 upgrade ✅
- Upgraded all three projects from net7.0 → net10.0
- Replaced Swashbuckle with native ASP.NET Core OpenAPI + Scalar UI (dev only)
- Fixed Swagger unconditionally enabled in production
- Removed `Microsoft.AspNetCore.Authentication` 2.2.0 (bundled since .NET Core 3)
- Removed unused imports

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
- Replaced static PNG ERDs with Mermaid diagrams in `docs/architecture.md`

---

## Upcoming phases

### Phase 4 — External OAuth ✅

Replace username/password login with external OAuth (Google initial provider).

- Added `Microsoft.AspNetCore.Authentication.Google`
- Provider-agnostic callback via `SignInManager.GetExternalLoginInfoAsync()` — adding a second provider (e.g. Entra ID) only requires a new `AddX()` call in `Program.cs`
- Login page enumerates configured providers dynamically via `GetExternalAuthenticationSchemesAsync()`
- On callback: find-or-create local user by email, check against `Authentication:AllowedEmails` config list
- Removed `IAuthService`, `AuthService`, `RegisterPage`, username/password login
- ASP.NET Identity retained as local user store (links external provider logins to local user record)
- Google credentials in user secrets (dev), Azure App Service config (prod)
- See `docs/google-oauth-setup.md` for Google Cloud Console setup steps

**Branch:** `feature/google-auth`

---

### Phase 5 — Client Management ✅

Add a shared `Clients` table replacing the free-text client field on projects.

- `Client` entity: `Name` (unique), `DefaultHourlyRate` (nullable, ex GST), `ContactName`, `ContactEmail`, `ContactPhone` (all nullable)
- `Project.ClientId` nullable FK — SET NULL on client delete; service layer blocks delete when active projects exist
- Clients shared across all users — no per-user scoping
- Clients CRUD pages + nav link (Admin only)
- Project create/edit form includes client dropdown
- `IClientService` / `ClientService` / `ClientEndpoints` following VSA pattern
- 12 new service integration tests (51 total)

**Branch:** `feature/client-management`

---

### Phase 6 — MudBlazor UI uplift ✅

Replace Tailwind + Radzen + QuickGrid with MudBlazor. Mobile-first responsive design.

- `MudLayout` + responsive `MudNavMenu` drawer (phone and desktop)
- `MudDataGrid` replaces QuickGrid
- `MudDialog`, `MudTextField`, `MudSelect`, `MudDatePicker` for forms
- MudBlazor Snackbar replaces `Blazored.Toast`
- `MudChart` replaces Radzen year chart — stacked bar (non-billable/invoiced/uninvoiced)
- Tailwind CSS removed
- Custom tab bar (Day/Month/Year/Project) on Entries page
- Dynamic app bar title per route
- Bottom sheet components (EntrySheet, ProjectSheet, ClientSheet) with lazy-loading fix and grip pill
- ProjectCard: chevron navigation; edit moved to ProjectDetailPage FAB
- ProjectDetailPage: edit FAB + live reload; entry rows open EntrySheet; start/end date in stats
- ProjectsPage: archive/unarchive replaces delete (sets EndDate with date picker prompt)
- ClientDetailPage (new): stats, contact info, active projects list, edit FAB
- `ProjectDetails` table merged into `Projects` table (migration: `MergeProjectDetailsIntoProject`)
- `TimeEntry`: `InvoiceReference` + `InvoicedAt` fields added (migration: `AddInvoiceFieldsToTimeEntry`)
- EntryRow: green receipt icon tooltip for invoiced entries
- Reports: "Billable" KPI → "Uninvoiced"
- `ProjectColors.ForClient()` added for algorithmic client colours
- Dev seed/clear endpoints at `/api/dev/seed` and `/api/dev/clear` (dev only)

**Branch:** `feature/mudblazor-ui` — merged via PR #38

---

### Phase 7 — Security hardening

Applied before deployment.

- **Managed Identity** — App Service authenticates to Azure SQL without credentials; no username/password anywhere in the production stack
- **Least privilege** — app DB user has `db_datareader` + `db_datawriter` only; no DDL rights in prod
- **Azure SQL firewall** — Azure services only, no public internet access
- **Security response headers** — CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`
- **Rate limiting** — ASP.NET Core built-in rate limiting on auth endpoints
- **HTTPS + HSTS** enforced
- **Secrets audit** — no credentials in source control or `appsettings.json`

**Branch:** `feature/security-hardening`

---

### Phase 8 — Azure deployment + CI/CD

- **Azure SQL Database** — free offer (32GB, automatic backups, Managed Identity auth)
- **Azure App Service F1** — free fixed plan; throttles at limit, never charges overage; sleeps after 20 min idle
- **GitHub Actions** — push to `main` → build → publish → deploy

**Free tier guarantee:** Both Azure tiers are fixed plans. Exceeding limits throttles the app — no metered overage charges.

**Branch:** `feature/azure-deployment`

---

### Future — Zoho Books integration

TimeTracker will eventually integrate with Zoho Books to partially automate invoice generation based on tracked time entries.

The REST API layer (Phase 3) is retained specifically to support this. Direction TBD — push from TimeTracker or pull from Zoho.

---

## Phase dependency order

```
Phase 0 ✅ → Phase 1 ✅ → Phase 2 ✅ → Phase 3 ✅ → Phase 4 ✅ → Phase 5 ✅ → Phase 6 ✅ → Phase 7 → Phase 8 → Zoho integration
```

## Free tier summary

| Service | Plan | Limit | Overage behaviour |
|---------|------|-------|-------------------|
| Azure App Service | F1 Free | 60 CPU min/day, sleeps after 20 min idle | Throttled — no charge |
| Azure SQL Database | Free offer | 32GB data, 32GB backup | Throttled — no charge |
| Google OAuth | — | Unlimited personal use | — |
| GitHub Actions | Free | 2,000 min/month | Queued — no charge |
