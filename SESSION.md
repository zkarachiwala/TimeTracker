# Session handoff — 2026-06-14

## Current state
- Branch: `main`, clean (PR #113 open — SESSION.md chore, merge when ready)
- All high priority issues complete (PRs #109, #110, #112, issue #105 closed)
- All CI green

## Plan — medium priority (work next)

Work in order, one branch per item:

### Step 1 — Program.cs refactor
- **#111** 🟡 Extract service registrations and dev endpoints into extension methods
  - `AddApplicationAuth()` — auth/cookie/Google registration
  - `AddApplicationRateLimiting()` — rate limiter registration
  - `MapDevEndpoints()` — `/api/dev/login`, `/api/dev/seed`, `/api/dev/clear`
  - Move `GetConnectionString` into `Infrastructure/` class
  - Target: Program.cs ~50 lines

### Step 2 — Rate limiting
- **#100** 🟡 Add rate limiting to all mutating API endpoints (ASP.NET Core built-in RateLimiter)

### Step 3 — Session revocation
- **#103** 🟡 Session revocation via SecurityStamp

### Step 4 — Structured logging & monitoring
- **#97** 🟡 Structured logging (Serilog), APM (Application Insights), uptime monitoring (UptimeRobot), `/health` endpoint

### Step 5 — Security hardening
- **#101** 🟡 SQL Server Row-Level Security
- **#104** 🟡 Automated database backup export

## Backlog (low priority)
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
*Updated 2026-06-14. Next: Step 1 — #111 Program.cs refactor.*
