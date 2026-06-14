# Session handoff — 2026-06-14

## Current state
- Branch: `feat/structured-logging-97`, clean commit pending push
- PRs #114 (#111), #117 (#116), #118 (#100), #119 (#115), #120 (#103) all merged this session

## Completed this session
- ✅ #111 Program.cs refactor (PR #114)
- ✅ #116 Server-side pagination cap + timer dispose fix (PR #117)
- ✅ #100 Global rate limiting covering all endpoints (PR #118)
- ✅ D018 added to decisions.md, architecture.md updated
- ✅ #115 Cancellation tokens threaded through all service methods and EF Core calls (PR #119)
- ✅ #103 Session revocation via SecurityStamp (PR #120)
- ✅ #97 Structured logging (Serilog), /health endpoint (PR in progress)

## Plan — remaining medium priority (work in order)

### Step 5 — Security hardening
- **#101** 🟡 SQL Server Row-Level Security
- **#104** 🟡 Automated database backup export

## Backlog (low priority)
- **#95** 🟢 Database-backed user management
- **#96** 🟢 Staging environment (requires paid tier upgrade)
- **#102** 🟢 Email/password fallback + TOTP MFA
- **#121** 🟢 OpenTelemetry → Grafana Cloud APM (free-tier APM path; deferred from #97 due to cost constraint on App Insights)

## Manual steps outstanding (external, no code needed)
- UptimeRobot: create free account, add monitor for `https://timetracker.dzk.com.au/health`
- Azure: enable App Service log stream to capture Serilog stdout

## Active tech debt (genuine constraints, no action until paid tier)
| # | Item | ADR |
|---|------|-----|
| TD1 | Global WASM rendering | D001, D003 |
| TD2 | F1: single instance, no slots | D003 |
| TD4 | Azure SQL free (auto-pause) | D003 |
| TD6 | No staging environment | D016 |
| TD17 | `unsafe-inline` CSP (MudBlazor) | D002 |
| TD21 | Cloudflare free plan | D017 |
| TD23 | No APM / distributed tracing | D019 |

## How to resume
```bash
cd /home/zkarachiwala/repos/TimeTracker
git status
cat SESSION.md
```

---
*Updated 2026-06-14. Next: Step 5 (#101 RLS, #104 backup). Manual outstanding: UptimeRobot setup, Azure log stream.*
