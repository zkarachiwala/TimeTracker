# Session handoff — 2026-06-15

## Current state
- Branch: `feat/structured-logging-97` — all commits pushed, PR #122 ready to merge
- UptimeRobot: live and monitoring `https://timetracker-zak.azurewebsites.net/health`

## Completed this session
- ✅ D019 updated with UptimeRobot service selection rationale (comparison table)
- ✅ TD24 added then retired — Playwright inter-test hang was a stdout pipe deadlock in GlobalSetup, not a WASM/HTTP2 issue
- ✅ Playwright handler leak fixed in AuthTests, AuthenticatedPageTest, AuthenticatedDesktopPageTest
- ✅ Two access-denied assertions merged into one test
- ✅ GlobalSetup pipe deadlock fixed (`BeginOutputReadLine`/`BeginErrorReadLine` after `process.Start()`)
- ✅ Playwright suite passes clean

## Next session
1. User to merge PR #122 (`feat/structured-logging-97`)
2. Step 5 — Security hardening:
   - **#101** 🟡 SQL Server Row-Level Security
   - **#104** 🟡 Automated database backup export

## Backlog (low priority)
- **#95** 🟢 Database-backed user management
- **#96** 🟢 Staging environment (requires paid tier upgrade)
- **#102** 🟢 Email/password fallback + TOTP MFA
- **#121** 🟢 OpenTelemetry → Grafana Cloud APM

## Manual steps outstanding (external, no code needed)
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
*Updated 2026-06-15. Next: merge PR #122, then Step 5 (#101 RLS, #104 backup).*
