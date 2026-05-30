# TimeTracker

A personal timesheeting application built to replace Clockify. Tracks time entries against projects, provides year-view reporting, and will integrate with Zoho Books for invoice generation.

## Tech stack

- **[.NET 10](https://dotnet.microsoft.com/)** — ASP.NET Core + Blazor WebAssembly
- **[Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)** — SQL Server via EF Core
- **[ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)** — user management
- **[Radzen.Blazor](https://blazor.radzen.com/)** — year-view chart
- **[MudBlazor](https://mudblazor.com/)** — UI component library *(coming soon)*

## Running locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Start SQL Server

```bash
docker run \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  --name timetracker-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Set user secrets

```bash
cd TimeTracker.API
dotnet user-secrets set "DbUser" "sa"
dotnet user-secrets set "DbPassword" "YourStrong@Passw0rd"
```

### 3. Apply database migrations

```bash
cd TimeTracker.API
dotnet ef database update --context TimeTrackerDataContext
dotnet ef database update --context IdentityDataContext
```

### 4. Run

```bash
cd TimeTracker.API
dotnet run
```

App: `https://localhost:7006`
API docs (dev only): `https://localhost:7006/scalar/v1`

## Documentation

- [Architecture](docs/architecture.md) — current and future state, tech decisions
- [Roadmap](docs/roadmap.md) — phased implementation plan

## Adding EF Core migrations

```bash
cd TimeTracker.API
dotnet ef migrations add <MigrationName> --context TimeTrackerDataContext
dotnet ef migrations add <MigrationName> --context IdentityDataContext
```
