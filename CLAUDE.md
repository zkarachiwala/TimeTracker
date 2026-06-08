# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Git discipline

NEVER blow away uncommitted changes. Always commit or stash before switching branches, pulling, or doing any operation that might overwrite local changes. If you find yourself with a large pile of uncommitted changes, stop and deal with that first before writing new code.

NEVER commit to the main branch, and NEVER commit broken code. Always create a new branch for each logical unit of work, and only merge to main when it's fully done and verified working.

Commit after each verified working increment. Never accumulate more than one logical unit of change without committing. Before starting any work, run `git status` — if there is a large pile of uncommitted changes, stop and deal with that first before writing new code.

"Done" means committed, not just working.

## Commands

```bash
# Run the application (serves both API and Blazor WASM client)
cd TimeTracker.Web && dotnet run

# Build the solution
dotnet build TimeTracker.sln

# EF Core migrations (run from TimeTracker.Web)
cd TimeTracker.Web
dotnet ef migrations add <MigrationName> --context TimeTrackerDataContext
dotnet ef migrations add <MigrationName> --context IdentityDataContext
dotnet ef database update --context TimeTrackerDataContext
dotnet ef database update --context IdentityDataContext

# Set user secrets (required for local dev DB credentials)
cd TimeTracker.Web
dotnet user-secrets set "DbUser" "<username>"
dotnet user-secrets set "DbPassword" "<password>"
```

The app runs at `https://localhost:7006` (https) or `http://localhost:5019` (http). Swagger UI is available at `/swagger`.

## Architecture

This is a .NET 10 solution with three projects:

- **`TimeTracker.Web`** — ASP.NET Core host. Serves Blazor SSR pages and REST API endpoints from a single process. Organised as Vertical Slice Architecture — feature folders under `Features/`, no controller or repository layer.
- **`TimeTracker.Shared`** — Class library containing EF Core entities only (`Entities/`).
- **`TimeTracker.Tests`** — xUnit integration tests targeting service classes directly via EF Core InMemory. No running database required.

### Data layer

Two separate EF Core `DbContext`s both targeting the same SQL Server database (`TimeTrackerDb`):
- `TimeTrackerDataContext` — app schema (`app`): `TimeEntries`, `Projects`, `ProjectDetails`, `ProjectUsers`
- `IdentityDataContext` — identity schema (`id`): ASP.NET Identity tables

`Project` uses soft-delete (`SoftDeleteableEntity`). `TimeEntry` stores a `UserId` string rather than a navigation property to avoid cascade delete issues.

Mapster handles entity ↔ DTO mapping via per-feature `IRegister` classes scanned at startup.

### Architecture

Vertical Slice Architecture — no controllers, no repositories. Feature services (`ITimeEntryService`, `IProjectService`, `IAuthService`) are injected directly into Blazor pages and minimal API endpoints. `IUserContextService` extracts the current user's ID from `HttpContext` claims and scopes all queries per user.

### Authentication

Cookie-based auth via ASP.NET Identity (HTTP-only, Secure, SameSite=Strict) with Google OAuth. `CookieCredentialHandler` in `TimeTracker.Wasm` forwards the auth cookie with every WASM HTTP request (`BrowserRequestCredentials.Include`).

All WASM pages (`@page` components in `TimeTracker.Wasm`) must carry `@attribute [Authorize]` so the SSR `AuthorizeRouteView` redirects unauthenticated users before WASM loads. Each page's data-loading method must also catch `HttpRequestException` with status 401 and call `Nav.NavigateTo("/login", forceLoad: true)` to handle mid-session expiry.

OAuth challenge links must use `data-enhance-nav="false"` to force a full-page navigation. Without it, Blazor's enhanced navigation turns the click into a fetch, which follows the redirect to `accounts.google.com` and is blocked by the CSP (`connect-src 'self'`).

In development, DB credentials (`DbUser`, `DbPassword`) are injected via [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) and merged into the connection string at startup.

### Contracts and DTOs

All request/response DTOs live in `TimeTracker.Contracts/`. Never define DTOs inline in `TimeTracker.Web` or `TimeTracker.Wasm`. Mapster mapping is done via per-feature `IRegister` classes inside each `Features/` folder in `TimeTracker.Web`, scanned at startup.

### Security headers

`TimeTracker.Web/Infrastructure/SecurityHeadersMiddleware.cs` defines the Content Security Policy. Any new external script, font, style, or API connection requires a corresponding CSP directive addition — the current policy is strict (`connect-src 'self'`) and will silently block unlisted origins.

### Testing

**Unit tests** (`TimeTracker.Tests`): xUnit + EF Core InMemory. Every service-layer change or new feature must include test additions or updates in the same commit. No running database required.

**Playwright E2E tests** (`TimeTracker.Playwright`):
- Unauthenticated tests extend `PageTest` directly.
- Authenticated tests extend `AuthenticatedPageTest` (loads stored auth state from `playwright/.auth/user.json`).
- Write tests (tests that mutate data) must be guarded with:
  ```csharp
  if (!WriteTestsEnabled) Assert.Ignore("Write tests disabled — set PLAYWRIGHT_WRITE_TESTS=true to run locally");
  ```
  where `WriteTestsEnabled` checks `Environment.GetEnvironmentVariable("PLAYWRIGHT_WRITE_TESTS") == "true"`. Write tests are skipped in CI and run locally only.
- Target URL is controlled by `PLAYWRIGHT_BASE_URL` env var (defaults to the Azure deployment).
