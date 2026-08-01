# Session handoff — 2026-06-28

## Cert study
All DP-800 and AZ-400 learning issues have moved to the study-plans repo.
5 schema-change issues remain here: #208, #209, #258, #263, #267 — see study-plans SESSION.md for details.

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
