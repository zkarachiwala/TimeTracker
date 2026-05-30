# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the application (serves both API and Blazor WASM client)
cd TimeTracker.API && dotnet run

# Build the solution
dotnet build TimeTracker.sln

# EF Core migrations (run from TimeTracker.API)
cd TimeTracker.API
dotnet ef migrations add <MigrationName> --context TimeTrackerDataContext
dotnet ef migrations add <MigrationName> --context IdentityDataContext
dotnet ef database update --context TimeTrackerDataContext
dotnet ef database update --context IdentityDataContext

# Set user secrets (required for local dev DB credentials)
cd TimeTracker.API
dotnet user-secrets set "DbUser" "<username>"
dotnet user-secrets set "DbPassword" "<password>"
```

The app runs at `https://localhost:7006` (https) or `http://localhost:5019` (http). Swagger UI is available at `/swagger`.

## Architecture

This is a .NET 7 solution with three projects:

- **`TimeTracker.API`** — ASP.NET Core web host. Serves the REST API and hosts the Blazor WebAssembly client via `UseBlazorFrameworkFiles()`. All HTTP traffic goes through this single process.
- **`TimeTracker.Client`** — Blazor WebAssembly SPA. Calls the API using a relative `HttpClient` (no CORS needed since it's hosted by the same process). Uses Radzen.Blazor for charts and Tailwind CSS for styling.
- **`TimeTracker.Shared`** — Class library shared between API and Client. Contains EF entities (`Entities/`) and request/response DTOs (`Models/`).

### Data layer

Two separate EF Core `DbContext`s both targeting the same SQL Server database (`TimeTrackerDb`):
- `TimeTrackerDataContext` — app schema (`app`): `TimeEntries`, `Projects`, `ProjectDetails`, `ProjectUsers`
- `IdentityDataContext` — identity schema (`id`): ASP.NET Identity tables

`Project` uses soft-delete (`SoftDeleteableEntity`). `TimeEntry` stores a `UserId` string (ASP.NET Identity user ID) rather than a navigation property to avoid cascade delete issues.

Mapster handles entity↔DTO mapping. The one non-trivial mapping is `Project → ProjectResponse`, which flattens `ProjectDetails` fields, configured in `Program.cs::ConfigureMapster()`.

### API layer

Controllers → Services → Repositories pattern. `IUserContextService` extracts the current user's ID from `HttpContext` claims and is injected into services to scope queries per user.

- `TimeEntryController` — requires `[Authorize]` (any authenticated user)
- `ProjectController` — requires `[Authorize(Roles = "Admin")]`
- `LoginController` / `AccountController` — public endpoints for JWT auth and registration

### Authentication

JWT Bearer authentication. On login, the API issues a JWT containing the username, user ID, and roles. The Blazor client stores the token in `localStorage` via `Blazored.LocalStorage`. `AuthStateProvider` reads the token on each navigation, parses claims from the JWT payload, and sets `HttpClient.DefaultRequestHeaders.Authorization`.

In development, DB credentials (`DbUser`, `DbPassword`) are injected via [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) and merged into the connection string at startup.
