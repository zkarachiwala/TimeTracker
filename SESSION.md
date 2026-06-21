# Session handoff — 2026-06-21

## Current state
- Branch: `feature/issue-155-test-framework-migration` — ready for PR

## Completed this session
- ✅ **#155 Test framework migration** — fully implemented and committed:
  - Playwright NUnit → xUnit (`Microsoft.Playwright.Xunit`); `ICollectionFixture<AppFixture>`; `IAsyncLifetime`; `[Fact]`; `Xunit.SkippableFact`
  - New `TimeTracker.ComponentTests` project (xUnit + bUnit); `MudBlazorContext` base class; 22 component tests for `EntryRow` and `ProjectCard`
  - All docs updated: `playwright-strategy.md`, `architecture.md`, `decisions.md` (D026), `roadmap.md`, `CLAUDE.md`
  - 164 service tests + 22 component tests — all green
- ✅ **Plan updated** — Phase 12 added to `plan.md`
- ✅ **Global CLAUDE.md** — xUnit preference for all new .NET projects added

## Next session
- User to run Playwright suite to confirm E2E tests work under xUnit: `PLAYWRIGHT_WRITE_TESTS=true BROWSER= dotnet test TimeTracker.Playwright --logger "console;verbosity=normal" --blame-hang-timeout 60s`
- If Playwright green → raise PR for #155

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
*Updated 2026-06-21. #155 implementation complete, awaiting Playwright E2E run before PR.*
