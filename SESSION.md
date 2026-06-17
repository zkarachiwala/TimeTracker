# Session handoff — 2026-06-17

## Current state
- Branch: `main` — clean, all PRs merged

## Completed this session
- ✅ Playwright failures fixed — 12 tests were failing because Chromium aborted in-flight `api/timeentries/active` fetch when tests navigated away before `LoadData()` completed; added `"Tracking now" or "Start a timer"` visibility wait to three SetUps (PR #131)
- ✅ CodeQL alert #8 (CWE-359) resolved — removed `result.Status` enum value from auth log message (PR #132)
- ✅ **#104 Automated database backup** — nightly `.bacpac` export via GitHub Actions to private `TimeTracker-backups` repo (PRs #133, #134, #135)
  - Dedicated service principal (`timetracker-github-backup`) with OIDC, no stored secrets
  - Custom Azure RBAC role: firewall rule write/delete only, scoped to SQL server
  - SQL user: `db_owner` (required for `DBCC SHOW_STATISTICS` and RLS bypass during export)
  - Backups pushed to private `TimeTracker-backups` repo; files older than 30 days auto-purged
  - Fully documented in `docs/azure-deployment.md` → "Database Backup Setup" (Steps A–H)
- ✅ Stale content removed from `docs/azure-deployment.md` (obsolete Playwright auth state section)
- ✅ `docs/architecture.md` updated with backup row and RLS/audit trail entries
- ✅ **#32 Trash / soft-delete restore** (PR #140)
  - `TimeEntry` upgraded from `BaseEntity` to `SoftDeleteableEntity` (EF migration: `AddSoftDeleteToTimeEntry`)
  - Delete buttons added to `ProjectSheet` and `ClientSheet` (two-step confirmation, `Color.Error`)
  - Archive guard on `ClientSheet` — blocked if non-archived projects exist
  - New `/trash` admin page — restores deleted Projects, Clients, and Time Entries
  - Trash nav link added under Admin `AuthorizeView` in `NavMenu`
  - 130 tests passing
- ✅ **D022 documented** — `MigrateAsync()` at startup decision record added to `docs/decisions.md` (PR #141)

## Next session
- **#95** 🟡 Database-backed user management — planned as next target

## Backlog
- **#95** 🟡 Database-backed user management
- **#96** 🟢 Staging environment (requires paid tier upgrade)
- **#102** 🟢 Email/password fallback + TOTP MFA
- **#121** 🟢 OpenTelemetry → Grafana Cloud APM
- **#137** 🟡 Award rate feature — client-level rate flowing to projects; weekend/public holiday multiplier on time entries
- **#138** 🟢 Calendar view — monthly calendar showing time entries per day on the entries UI

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
*Updated 2026-06-17. All session work merged. No outstanding code changes.*
