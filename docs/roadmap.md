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

---

## Upcoming phases

### Phase 3 — Blazor SSR + Vertical Slice Architecture

The largest structural change. Collapses the WASM client into the API project, migrates to Blazor SSR, and restructures to feature-organised vertical slices.

**Architecture principles:**
- Feature folders — each feature is self-contained (service interface, implementation, DTOs, endpoints, pages)
- No repository layer — `DbContext` injected directly into feature services (EF Core is the repository)
- Interfaces on all services — `AddScoped<ITimeEntryService, TimeEntryService>()`
- No MediatR — plain feature services with interfaces, injected directly into Blazor components
- DTOs stay — entities are not exposed to the UI layer or API consumers; DTOs live in feature-scoped `*Models.cs` files
- REST API retained — minimal API endpoints alongside Blazor pages, backed by the same services, for future Zoho Books integration
- Mapster stays — moves from global `Program.cs` config to per-feature mapping

**Target structure:**
```
TimeTracker.Web/
  Features/
    TimeEntries/
      ITimeEntryService.cs
      TimeEntryService.cs
      TimeEntryModels.cs
      TimeEntryEndpoints.cs
      Pages/
        TimeEntriesPage.razor
        EditTimeEntryPage.razor
    Projects/
      IProjectService.cs
      ProjectService.cs
      ProjectModels.cs
      ProjectEndpoints.cs
      Pages/
        ProjectsPage.razor
        EditProjectPage.razor
    Auth/
      IAuthService.cs
      AuthService.cs
      Pages/
        LoginPage.razor
  Shared/
    IUserContextService.cs
    UserContextService.cs
    Layout/
      MainLayout.razor
      NavMenu.razor
TimeTracker.Shared/    ← entities only
```

**Removed in this phase:**
- `TimeTracker.Client` project
- Controller layer
- Repository layer
- `HttpClient` calls from pages
- `AuthStateProvider`, `Blazored.LocalStorage`, `Blazored.Toast`
- DTOs from `TimeTracker.Shared` (moved to feature folders)

**Branch:** `feature/blazor-ssr`

---

### Phase 4 — Google OAuth + cookie auth

Replace username/password JWT with Google OAuth. HTTP-only cookies replace JWT-in-localStorage.

- `Microsoft.AspNetCore.Authentication.Google` + cookie auth middleware
- ASP.NET Identity retained as local user store (stores Google sub + email)
- On OAuth callback: find or create local user by email, sign in via cookie
- Remove `JwtBearer`, all JWT config, login/account controllers and services
- Google credentials in user secrets (dev), Azure App Service config (prod)

**Security:** HTTP-only + Secure + SameSite=Strict cookies, CSRF via Blazor SSR antiforgery, OAuth state parameter validated.

**Branch:** `feature/google-auth`

---

### Phase 5 — MudBlazor UI uplift

Replace Tailwind + Radzen + QuickGrid with MudBlazor. Mobile-first responsive design.

- `MudLayout` + responsive `MudNavMenu` drawer (phone and desktop)
- `MudDataGrid` replaces QuickGrid
- `MudDialog`, `MudTextField`, `MudSelect`, `MudDatePicker` for forms
- MudBlazor Snackbar replaces `Blazored.Toast`
- `MudChart` evaluated as replacement for Radzen year chart
- Tailwind CSS removed

Can be combined with Phase 3 into one PR.

**Branch:** `feature/mudblazor-ui`

---

### Phase 6 — Security hardening

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

### Phase 7 — Azure deployment + CI/CD

- **Azure SQL Database** — free offer (32GB, automatic backups, Managed Identity auth)
- **Azure App Service F1** — free fixed plan; throttles at limit, never charges overage; sleeps after 20 min idle
- **GitHub Actions** — push to `main` → build → publish → deploy

**Free tier guarantee:** Both Azure tiers are fixed plans. Exceeding limits throttles the app — no metered overage charges.

**Branch:** `feature/azure-deployment`

---

### Future — Zoho Books integration

TimeTracker will eventually integrate with Zoho Books to partially automate invoice generation, replacing the current manual Clockify → Zoho Books export.

The REST API layer (Phase 3) is retained specifically to support this. Direction TBD — push from TimeTracker or pull from Zoho.

---

## Phase dependency order

```
Phase 0 ✅ → Phase 1 ✅ → Phase 2 ✅ → Phase 3+5 → Phase 4 → Phase 6 → Phase 7 → Zoho integration
```

## Free tier summary

| Service | Plan | Limit | Overage behaviour |
|---------|------|-------|-------------------|
| Azure App Service | F1 Free | 60 CPU min/day, sleeps after 20 min idle | Throttled — no charge |
| Azure SQL Database | Free offer | 32GB data, 32GB backup | Throttled — no charge |
| Google OAuth | — | Unlimited personal use | — |
| GitHub Actions | Free | 2,000 min/month | Queued — no charge |
