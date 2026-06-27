# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project purpose

This project has two equally important goals:

1. **Practical app** — a real replacement timetracking app with free hosting as a hard constraint. Cost decisions are real and binding.
2. **Learning exercise** — the developer is actively building their skills and learning best practice through this project.

When reviewing tech debt, making recommendations, or proposing solutions: consider educational value alongside practical necessity. Something that isn't strictly required for a personal app may still be worth doing if it teaches a valuable, transferable skill. Always verify whether recommended tools have a genuine free tier before suggesting them.

## GitHub issues

Every GitHub issue created for this repository must be:
1. Added to the **Timetracker** project board (project #1, owner `zkarachiwala`)
2. Given a **Priority** label (🔴 High / 🟡 Medium / 🟢 Low) via the project Priority field
3. Given appropriate **labels** from: `security`, `infrastructure`, `observability`, `enhancement`, `bug`, `documentation`

```bash
# Add issue to project
gh project item-add 1 --owner zkarachiwala --url https://github.com/zkarachiwala/TimeTracker/issues/<number>
```

## Remote sessions (Claude Code on the web)

`.claude/hooks/session-start.sh` installs .NET 10 via apt, restores NuGet packages, and builds the solution automatically at the start of every remote session. This means the fast test suite works in remote sessions without any manual setup.

The hook only runs when `CLAUDE_CODE_REMOTE=true` — it is a no-op locally.

## Dev container (primary dev environment)

The project includes a VS Code dev container backed by Docker Compose. This is the preferred development environment — see `docs/devcontainer-guide.md` for full explanation of the concepts.

**First-time setup:**
1. Copy `.env.example` to `.env` and fill in `SA_PASSWORD`, `DB_USER`, `DB_PASSWORD`
2. Open the repo in VS Code — it will offer to reopen in the container
3. `postCreateCommand` runs automatically: restores packages and applies EF migrations

**First-time setup:**
1. Copy `.env.example` to `.env` and fill in `SA_PASSWORD`, `DB_USER`, `DB_PASSWORD`
2. Open the repo in VS Code — it will offer to reopen in the container
3. `postCreateCommand` runs automatically: installs EF tools, restores packages, applies migrations
4. `postStartCommand` runs automatically: starts the app on `http://localhost:5019`

**Running the app inside the container:**

The app starts automatically via `postStartCommand` on container start. To restart manually:
- `Ctrl+Shift+P` → **Tasks: Run Task** → **run**, or press `F5` to launch with debugger attached

Do NOT use `--launch-profile https` or `--launch-profile http` inside the container — they bind to `localhost` only (unreachable from outside). The `container` profile uses `http://+:5019` (all interfaces).

**Google OAuth:** Add `http://localhost:5019/signin-google` to Authorized Redirect URIs in Google Cloud Console alongside the production URI. Google credentials come from .NET User Secrets — no `.env` entry needed.

**Useful Docker Compose commands (run from host or container terminal):**
```bash
docker compose up -d          # Start all services
docker compose down           # Stop (data preserved)
docker compose down -v        # Stop and wipe volumes (clean slate)
docker compose logs db        # SQL Server logs
```

**Three SQL Server instances in this project** — they serve different purposes and never all run simultaneously:
- Local SQL Server (existing) — local dev outside the container
- Dev container SQL Server — SQL Server 2025 Developer edition inside the dev container (Docker)
- Testcontainers SQL Server — spun up only during `Category=Container` tests

**SQL Server 2025 features in use (dev container only):** native `json` data type, `vector` data type, `AI_GENERATE_EMBEDDINGS`, `AI_GENERATE_CHUNKS`, `sp_invoke_external_rest_endpoint`, `REGEXP_*` functions, `EDIT_DISTANCE*`/`JARO_WINKLER_*` fuzzy functions, vector indexes, and Data API Builder / SQL MCP Server. Several of these require `PREVIEW_FEATURES = ON` at the database level — see the DP-800 issues (#208–#223) for setup details per feature.

**Ollama** is the local embedding and chat model provider. It runs on the Windows host; SQL Server 2025 reaches it via `host.docker.internal:11434`. Pull models before working on AI issues: `ollama pull nomic-embed-text` (embeddings) and `ollama pull llama3.2` (chat/RAG).

## Standard test commands

**Before every PR** — run the full solution (service tests + bUnit + Playwright E2E):
```bash
PLAYWRIGHT_WRITE_TESTS=true BROWSER= dotnet test TimeTracker.sln --logger "console;verbosity=normal" --blame-hang-timeout 60s
```
Prerequisite: SQL Server running, user secrets set (`DbUser`, `DbPassword`). **Local only — not available in remote sessions.**

**During development** (fast feedback, no DB or Docker needed):
```bash
dotnet test TimeTracker.Tests --filter "Category!=Container" && dotnet test TimeTracker.ComponentTests
```
Works in remote sessions. The `Category!=Container` filter excludes Testcontainers-based tests (RLS + migration smoke tests), which require Docker.

**Container tests only** (requires Docker locally or CI):
```bash
dotnet test TimeTracker.Tests --filter "Category=Container"
```

**All unit + container tests** (requires Docker):
```bash
dotnet test TimeTracker.Tests && dotnet test TimeTracker.ComponentTests
```

**Hang diagnostic only** (not part of normal suite):
```bash
PLAYWRIGHT_HANG_DIAGNOSTIC=true BROWSER= dotnet test TimeTracker.Playwright --filter "FullyQualifiedName~HangDiagnosticTests" --blame-hang-timeout 60s
```

## Playwright tests

`stopOnFail` and `parallelizeTestCollections: false` are configured in `TimeTracker.Playwright/xunit.runner.json` — no extra flags needed.

The pre-push hook (`.githooks/pre-push`) is intentionally disabled. Never re-enable it — it caused repeated background process incidents by blocking `git push` for 2–3 minutes.

### Playwright failure triage — MANDATORY before any code change

When a Playwright test fails, answer these two questions FIRST. Do not touch any code until both are answered.

**1. Where is the error coming from?**
- `AssertNoConsoleErrors` failure with `"Request failed: <url>"` → this is a **browser console message** written by Blazor when an `HttpRequestException` goes unhandled. Fix is in the **app** (`LoadData()` catch block), not in the test.
- `AssertNoConsoleErrors` failure with other text → a real Blazor/JS console error. Fix in the app.
- `WaitForRequestFinishedAsync` timeout → the API call never completed. Check the server.
- Element not found / assertion failed → `InitializeAsync` navigation or wait is wrong.

**2. Is this teardown or mid-test?**
- If the failed URL is from the timer page (`/api/timeentries/active`, `/api/timeentries/today`) and the test doesn't interact with the timer, it's almost certainly a **teardown abort** — the page closed while a request was still in-flight.
- Teardown aborts are fixed in the **app** by catching all `HttpRequestException` in `LoadData()`, not just `Unauthorized`. An aborted request has `StatusCode == null` and does not match the `when` guard.

**Never change a test to make a failing test pass. Fix the app or the SetUp.**

Full strategy and wait patterns: `docs/testing-strategy.md` (Part 1).

Showcase smoke tests run against a locally-served static build: `BROWSER= dotnet test TimeTracker.Playwright --filter "FullyQualifiedName~ShowcaseTests" --blame-hang-timeout 60s`

## Dependency management

### Dependabot

Dependabot runs weekly (Monday) for NuGet and GitHub Actions packages.

**EF Core packages must always be upgraded together.** They have a tight lock-step version requirement: upgrading only one package causes `NU1605` (package downgrade as error) because the upgraded package pulls in a newer `Microsoft.EntityFrameworkCore.Relational`, which then requires a newer `Microsoft.EntityFrameworkCore` than what the other projects still pin. A Dependabot `group` named `entity-framework-core` in `.github/dependabot.yml` ensures all EF Core packages are bundled into one PR automatically. If you ever need to upgrade EF Core manually, update all of these in one commit across all three projects (Web, Tests, Shared):
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.InMemory` (Tests only)

### bUnit upgrades

bUnit follows semantic versioning strictly. Major version bumps (e.g. 1.x → 2.x) are breaking. Known breaking changes when upgrading to bUnit 2.x:
- `RenderComponent<T>()` → `Render<T>()` (all call sites in test files and `MudBlazorContext`)
- `TestContext` → `BunitContext` (kept as `[Obsolete]` shim in 2.x)
- `MudBlazorContext` must implement `IAsyncLifetime` so xUnit uses async teardown — MudBlazor services (`PopoverService`, `KeyInterceptorService`) are `IAsyncDisposable`-only and throw if synchronously disposed. See `TimeTracker.ComponentTests/Fixtures/MudBlazorContext.cs` for the pattern.

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

**Component tests** (`TimeTracker.ComponentTests`): xUnit + bUnit. Extend `MudBlazorContext` for components that use MudBlazor. No running database or browser required.

**Playwright E2E tests** (`TimeTracker.Playwright`): xUnit + `Microsoft.Playwright.Xunit`.
- Unauthenticated tests extend `PageTest` directly with `[Collection("App")]`.
- Authenticated tests extend `AuthenticatedPageTest` or `AuthenticatedDesktopPageTest` (loads stored auth state from `playwright/.auth/user.json`).
- One-time app startup is handled by `AppFixture` (`ICollectionFixture<AppFixture>`) via the `[Collection("App")]` attribute.
- Write tests (tests that mutate data) must be guarded with `[SkippableFact]` + `Skip.If(!WriteTestsEnabled, "...")`. Write tests are skipped in CI and run locally only.
- Target URL is controlled by `PLAYWRIGHT_BASE_URL` env var (defaults to the Azure deployment).

**Showcase Playwright tests** (`TimeTracker.Playwright`, `[Collection("Showcase")]`): smoke-test every routed page of the GitHub Pages showcase. `ShowcaseFixture` publishes the showcase and serves it via ASP.NET Core static files on port 7008. No auth state needed — `MockAuthenticationStateProvider` always returns Admin. Run with `--filter "FullyQualifiedName~ShowcaseTests"`.
