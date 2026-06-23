# Session handoff — 2026-06-23

## Completed this session
- ✅ **#161 Testcontainers for RLS and migration tests** — merged as PR #192
- ✅ **#162 Dev container and Docker Compose** — PR #194 raised, working end-to-end

## Dev container — how it works now
- Open VS Code in the repo → click "Reopen in Container" (bottom-left `><` menu)
- App starts automatically at `http://localhost:5019`
- Google OAuth credentials come from WSL User Secrets (mounted read-only)
- Docker panel in VS Code sidebar manages containers
- To seed mock data: log in → POST `/api/dev/seed` via Swagger at `/swagger`
- Data persists in named volume — only wiped by `docker compose down -v`

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

## Known gotcha: build artifacts
The container writes `obj/bin` files owned by the container user. Running tests from the host while the container is active fails with MSB3492 errors. Fix: stop the container first, or clean artifacts via Docker:
```bash
docker run --rm -v $(pwd):/workspace alpine sh -c "rm -rf /workspace/TimeTracker.*/obj /workspace/TimeTracker.*/bin"
```

## Backlog (from GitHub project board)

### 🟡 Medium
| # | Title |
|---|-------|
| ~~#161~~ | ~~Add Testcontainers for RLS and migration tests~~ ← merged PR #192 |
| #162 | Add dev container and Docker Compose ← **PR #194 pending review** |
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
| #163 | Define Azure infrastructure as code with Bicep |
| #102 | Add email/password login fallback and TOTP MFA |
| #96  | Add staging environment (requires paid tier upgrade) |

## Active tech debt
| # | Item | ADR |
|---|------|-----|
| TD1 | Global WASM rendering | D001, D003 |
| TD2 | F1: single instance, no slots | D003 |
| TD4 | Azure SQL free (auto-pause) | D003 |
| TD6 | No staging environment | D016 |
| TD17 | `unsafe-inline` CSP (MudBlazor) | D002 |
| TD21 | Cloudflare free plan | D017 |
| TD23 | No APM / distributed tracing | D019 |
| TD25 | Award rate jurisdiction hardcoded to national AU | D025 |

## How to resume
```bash
cd /home/zkarachiwala/repos/TimeTracker
git status
cat SESSION.md
```

---
*Updated 2026-06-23. PR #194 raised for #162. App running in dev container.*
