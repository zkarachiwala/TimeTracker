# Session handoff — 2026-06-20

## Current state
- Branch: `feature/issue-146-nav-rail` — ready to test and PR

## Completed this session
- ✅ **PR #150 merged** — #138 Calendar view
- ✅ **#146 Navigation rail** — `DrawerVariant.Mini` on desktop/tablet (always-visible 56px icon rail, expands on hover/click); hamburger hidden on desktop via `MudHidden`; drawer footer removed (logout in avatar dropdown); mobile unchanged; `IBrowserViewportService` drives responsive variant switching; Playwright desktop nav tests updated (no more `OpenDrawer()`)

## Next session
- Run Playwright suite: `PLAYWRIGHT_WRITE_TESTS=true BROWSER= dotnet test TimeTracker.Playwright --logger "console;verbosity=normal"`
- PR for #146 once Playwright is green
- Look at fix/issue-151 (showcase users DI) — has uncommitted changes stashed on main (`git stash pop` when switching to that branch)

## Backlog
- **#96** 🟢 Staging environment (requires paid tier upgrade)
- **#102** 🟢 Email/password fallback + TOTP MFA
- **#121** 🟢 OpenTelemetry → Grafana Cloud APM
- **#151** 🔴 Showcase users DI fix

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
*Updated 2026-06-20. Branch `feature/issue-146-nav-rail` ready to test.*
