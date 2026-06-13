# Session handoff — 2026-06-13

## Current state
- Branch: `main`, clean
- All CI green

## Plan — high priority issues (all complete ✅)

### Step 1 — Quick wins ✅ PR #109
- **#99** Restrict `AllowedHosts` to `timetracker.dzk.com.au;timetracker-zak.azurewebsites.net`
- **#98** Add global exception handler: `app.UseExceptionHandler` + `IProblemDetailsService`
- Fixed Playwright tests to start isolated app instance on port 7007

### Step 2 — CI hardening ✅ PR #110
- **#94** `dotnet list package --vulnerable` in CI, GitHub secret scanning + push protection enabled

### Step 3 — Connection pool ✅ PR #112
- **#93** `Max Pool Size=30` per pool (60 total), `Min Pool Size=0` for auto-pause

### Step 4 — Managed Identity ✅ issue #105 closed
- **#105** Was already fully configured in Azure (MI enabled, Entra admin set, T-SQL done, connection strings updated, credentials removed)

## Backlog (medium/low priority — do after above)
- **#97** 🟡 Structured logging (Serilog), APM (Application Insights), uptime monitoring (UptimeRobot), `/health` endpoint
- **#100** 🟡 Rate limiting on all mutating endpoints (ASP.NET Core built-in RateLimiter)
- **#101** 🟡 SQL Server Row-Level Security
- **#103** 🟡 Session revocation via SecurityStamp
- **#104** 🟡 Automated database backup export
- **#95** 🟢 Database-backed user management
- **#96** 🟢 Staging environment (requires paid tier upgrade)
- **#102** 🟢 Email/password fallback + TOTP MFA

## Active tech debt (genuine constraints, no action until paid tier)
| # | Item | ADR |
|---|------|-----|
| TD1 | Global WASM rendering | D001, D003 |
| TD2 | F1: single instance, no slots | D003 |
| TD4 | Azure SQL free (auto-pause) | D003 |
| TD6 | No staging environment | D016 |
| TD17 | `unsafe-inline` CSP (MudBlazor) | D002 |
| TD21 | Cloudflare free plan | D017 |

## How to resume
```bash
cd /home/zkarachiwala/repos/TimeTracker
git status
cat SESSION.md
```

---
*Updated 2026-06-14. All high priority issues complete. Next: backlog items or #111 (Program.cs refactor).*
