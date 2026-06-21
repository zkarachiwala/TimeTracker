# Session handoff — 2026-06-21

## Current state
- Branch: `feature/issue-146-nav-rail` — PR raised, all 61 Playwright tests green

## Completed this session
- ✅ **#146 Navigation rail** — Playwright suite green; PR raised
- ✅ **Playwright hang investigation** — root causes identified and fixed:
  - `GlobalSetup.TearDown`: added `CancelOutputRead/Error` before `Kill(entireProcessTree:true)` to prevent pipe-buffer deadlock
  - `GlobalSetup.AuthenticateAsync`: explicit disposal scope (request before playwright driver)
  - `SetDefaultTimeout(30_000)` in both base classes
  - `PageTearDownAsync` with `WaitAsync(10s)` cap in both base classes
  - `--blame-hang-timeout 60s` added to run command as process-level safety net
  - `HangDiagnosticTests` fixture preserved as `[Explicit]` for future teardown testing
- ✅ **xUnit migration issue raised** — covers Playwright NUnit → xUnit + bUnit component tests

## Next session
- Merge PR #146 when checks pass
- Fix/issue-151 (showcase users DI) — stashed changes on main (`git stash pop` when switching)

## Backlog
- **#96** 🟢 Staging environment (requires paid tier upgrade)
- **#102** 🟢 Email/password fallback + TOTP MFA
- **#121** 🟢 OpenTelemetry → Grafana Cloud APM
- **#151** 🔴 Showcase users DI fix
- **xUnit migration** 🟢 Playwright NUnit → Microsoft.Playwright.Xunit + bUnit component tests

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
*Updated 2026-06-21. PR for #146 raised.*
