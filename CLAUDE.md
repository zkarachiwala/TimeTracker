# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

Cookie-based auth via ASP.NET Identity (HTTP-only, Secure, SameSite=Strict). Username/password login is temporary — Phase 4 replaces it with Google OAuth.

In development, DB credentials (`DbUser`, `DbPassword`) are injected via [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) and merged into the connection string at startup.
