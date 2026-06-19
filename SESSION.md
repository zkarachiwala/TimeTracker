# Session handoff — 2026-06-18

## Current state
- Branch: `feature/issue-137-award-rate` — in progress, all changes committed after this session
- Plan file: `docs/plans/issue-137-award-rate.md`

## Completed this session
- ✅ Playwright failures fixed — 12 tests were failing because Chromium aborted in-flight `api/timeentries/active` fetch when tests navigated away before `LoadData()` completed; added `"Tracking now" or "Start a timer"` visibility wait to three SetUps (PR #131)
- ✅ CodeQL alert #8 (CWE-359) resolved — removed `result.Status` enum value from auth log message (PR #132)
- ✅ **#104 Automated database backup** — nightly `.bacpac` export via GitHub Actions to private `TimeTracker-backups` repo (PRs #133, #134, #135)
- ✅ **#32 Trash / soft-delete restore** (PR #140)
- ✅ **D022 documented** — `MigrateAsync()` at startup decision record added to `docs/decisions.md` (PR #141)
- ✅ **#95 Database-backed user management** (PR #144)
- ✅ **#137 Award rate** — all phases complete on this branch:
  - D025 added to `docs/decisions.md` — `PublicHoliday` NuGet chosen (Nager.Date rejected: requires paid license key)
  - TD25 added to `docs/technical-debt.md` — jurisdiction hardcoded to national AU pending external investigation
  - `Client` entity: `AwardRate (decimal?)` added + EF migration `AddAwardRateToClient`
  - `ClientResponse`, `ClientRequest`, `ClientCreateRequest`, `ClientUpdateRequest` in Contracts updated
  - `ClientService` create/update wired through
  - `ClientSheet.razor` — "Award rate (AUD)" field added
  - `MockClientService` and `MockDataStore` seed data updated
  - `IAwardRateResolver` / `AwardRateResolver` using `PublicHoliday.AustraliaPublicHoliday` (national holidays + weekends)
  - `TimeEntryResponse` — `EffectiveRate (decimal?)` and `IsAwardRate (bool)` added with defaults
  - `TimeEntryService` — injects resolver, `.ThenInclude(p => p.Client)`, enriches all returned entries
  - `EntryRow.razor` — "AW" badge shown when `IsAwardRate == true`
  - 164/164 tests green

## Next session
- **#138** 🟢 Calendar view

## Backlog
- **#96** 🟢 Staging environment (requires paid tier upgrade)
- **#102** 🟢 Email/password fallback + TOTP MFA
- **#121** 🟢 OpenTelemetry → Grafana Cloud APM
- **#138** 🟢 Calendar view — monthly calendar showing time entries per day

## Active tech debt (genuine constraints)
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
*Updated 2026-06-18. Branch `feature/issue-137-award-rate` ready to PR.*
