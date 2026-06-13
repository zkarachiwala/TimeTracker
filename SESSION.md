# Session handoff — 2026-06-13

## Current state
- Branch: `main`, clean
- All CI green

## Plan — high priority issues (work next)

Work these in order, one branch per item (except step 1 which bundles two quick wins):

### Step 1 — Quick wins (single PR)
- **#99** Restrict `AllowedHosts` to `timetracker.dzk.com.au;timetracker-zak.azurewebsites.net` in `appsettings.json`
- **#98** Add global exception handler: `app.UseExceptionHandler` + `IProblemDetailsService` (RFC 7807 Problem Details)

### Step 2 — CI hardening
- **#94** Add SAST/dependency scanning: `dotnet list package --vulnerable` step in CI, enable GitHub secret scanning and Dependabot

### Step 3 — Connection pool
- **#93** Set explicit `Min Pool Size` / `Max Pool Size` in connection strings (Azure SQL free tier: max 75 concurrent logins)

### Step 4 — Managed Identity (most complex, do last)
- **#105** Replace `DbUser`/`DbPassword` App Service settings with Managed Identity authentication
  - Enable system-assigned Managed Identity on App Service
  - Set Entra ID admin on Azure SQL logical server
  - Run T-SQL: `CREATE USER [<app-name>] FROM EXTERNAL PROVIDER` + grant roles
  - Add NuGet: `Microsoft.Data.SqlClient.Extensions.Azure` 7.0.0+
  - Update production connection strings to `Authentication=Active Directory Default`
  - Remove `DbUser` and `DbPassword` from App Service settings
  - Local dev (user secrets) unchanged

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
*Updated 2026-06-13. Next: Step 1 — issues #99 + #98.*
