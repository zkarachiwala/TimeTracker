# Session handoff — 2026-10-17

## Last session (2026-10-17)
**Shipped (all merged to `main`, full suite green with no exclusions):**
| PR | Issues | What |
|----|--------|------|
| #433 | #430, #431 | Time zone fix — entries before 11:00 AEDT showed under yesterday. Client groups by local date (`LocalDates`, `TimeEntryFilters`); `/api/timeentries/today` and `/year/{year}/all` now take a required `?tz=` IANA zone and query a UTC range (`LocalDateRange`). See ADR-038 |
| #435 | #434 | Security: .NET 2026-10 advisories — all ASP.NET Core/EF Core packages 10.0.12 → 10.0.13, plus direct `System.Security.Cryptography.Pkcs` 10.0.13 override (SqlClient 6.1.7 still pulls vulnerable 9.0.11) |
| #436 | #432 | Calendar: whole day cell now clickable (stretched-link CSS in `app.css`); value bars still drill into their project |

**Open follow-ups:**
- **Verify on Azure after deploy:** timer page loads its "today" list — first live test of IANA zone resolution on App Service. If broken: 400 from `/api/timeentries/today?tz=…` in the browser console
- **Paginated query endpoints** (`/year|month|day/.../{skip}/{limit}`, `ITimeEntryQueryService`) still use UTC date parts — unused by the UI; noted in ADR-038, not yet its own issue
- **Remove `Pkcs` override** in `TimeTracker.Web.csproj` once Microsoft.Data.SqlClient ships a patched dependency
- **Remove calendar CSS workaround** in `app.css` if upstream fixes [danheron/Heron.MudTotalCalendar#43](https://github.com/danheron/Heron.MudTotalCalendar/issues/43)
- **Open PR #342** — EF Core tooling least-privilege credentials (pre-existing)

## Cert study
All DP-800 and AZ-400 learning issues have moved to the study-plans repo.
The 5 schema-change issues that remained here (#208, #209, #258, #263, #267) are all closed.

## Standard test commands
**Before every PR:**
```bash
PLAYWRIGHT_WRITE_TESTS=true BROWSER= dotnet test TimeTracker.sln --logger "console;verbosity=normal" --blame-hang-timeout 60s
```
**Fast (no Docker or browser):**
```bash
dotnet test TimeTracker.Tests --filter "Category!=Container" && dotnet test TimeTracker.ComponentTests
```
**Container tests only (requires Docker):**
```bash
dotnet test TimeTracker.Tests --filter "Category=Container"
```
**Showcase smoke tests:**
```bash
BROWSER= dotnet test TimeTracker.Playwright --filter "FullyQualifiedName~ShowcaseTests" --logger "console;verbosity=normal" --blame-hang-timeout 60s
```

**Gotcha — Playwright "Executable doesn't exist":** after Dependabot bumps `Microsoft.Playwright.Xunit`, install the matching browser:
```bash
pwsh TimeTracker.Playwright/bin/Debug/net10.0/playwright.ps1 install chromium
```

## Dev container
- Open VS Code → "Reopen in Container"
- App starts automatically at `http://localhost:5019`
- Google OAuth credentials come from WSL User Secrets (mounted read-only into the container)
- To seed mock data: log in → POST `/api/dev/seed` via Swagger at `/swagger`
- Data persists in named volume — only wiped by `docker compose down -v`

**Known gotcha:** Container writes `obj/bin` files. Running tests from host while container is active fails with MSB3492. Fix: stop container first, or:
```bash
docker run --rm -v $(pwd):/workspace alpine sh -c "rm -rf /workspace/TimeTracker.*/obj /workspace/TimeTracker.*/bin"
```

## App backlog

### 🟡 Medium
| # | Title |
|---|-------|
| #166 | CSV export for time entries |
| #167 | Project budget tracking |
| #121 | Add distributed tracing and APM via OpenTelemetry and Grafana Cloud |

### 🟢 Low
| # | Title |
|---|-------|
| #168 | Duplicate time entry |
| #169 | Tags for time entries |
| #170 | Time rounding rules |
| #36  | Invoice export — uninvoiced entries per client for Zoho Books |
| #108 | Run Lighthouse audit and address findings |
| #102 | Add email/password login fallback and TOTP MFA |
| #96  | Add staging environment (requires paid tier upgrade) |

## Active tech debt
| # | Item | ADR |
|---|------|-----|
| TD1 | Global WASM rendering | ADR-001, ADR-003 |
| TD2 | F1: single instance, no slots | ADR-003 |
| TD4 | Azure SQL free (auto-pause) | ADR-003 |
| TD6 | No staging environment | ADR-016 |
| TD17 | `unsafe-inline` CSP (MudBlazor) | ADR-002 |
| TD21 | Cloudflare free plan | ADR-017 |
| TD23 | No APM / distributed tracing | ADR-019 |
| TD25 | Award rate jurisdiction hardcoded to national AU | ADR-025 |

## How to resume
```bash
cd /home/zkarachiwala/repos/TimeTracker
git status
cat SESSION.md
```
