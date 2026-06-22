# Session handoff — 2026-06-22

## Completed this session
- ✅ **#187 Dependabot batch** — consolidated 5 conflicting Dependabot PRs into one:
  - `Microsoft.NET.Test.Sdk` 17.14.0 → 18.6.0
  - `Microsoft.Playwright.Xunit` 1.52.0 → 1.60.0
  - `xunit.runner.visualstudio` 2.8.2 → 3.1.5
  - `Xunit.SkippableFact` 1.4.13 → 1.5.61
  - `Scalar.AspNetCore` 2.5.3 → 2.16.4
- ✅ **#188 Showcase CSS sync + smoke tests** (issue #159):
  - Deleted `showcase-app.css`; MSBuild now copies `app.css` from `TimeTracker.Web` directly
  - `ShowcaseFixture` + `ShowcaseTests` — 7 smoke tests, all passing
  - Root cause of `.dat` 404: `UseStaticFiles` silently refuses unknown MIME types; fixed with `ServeUnknownFileTypes = true`
  - `docs/playwright-strategy.md` → `docs/testing-strategy.md` (3 parts: E2E, Showcase, bUnit)

## Standard test commands
**Before every PR:**
```bash
PLAYWRIGHT_WRITE_TESTS=true BROWSER= dotnet test TimeTracker.sln --logger "console;verbosity=normal" --blame-hang-timeout 60s
```
**Showcase tests only:**
```bash
BROWSER= dotnet test TimeTracker.Playwright --filter "FullyQualifiedName~ShowcaseTests" --logger "console;verbosity=normal" --blame-hang-timeout 60s
```
**During development (fast):**
```bash
dotnet test TimeTracker.Tests && dotnet test TimeTracker.ComponentTests
```

## Backlog
- **#151** 🔴 Showcase users DI fix (routing/display issues in showcase)
- **#96** 🟢 Staging environment (requires paid tier upgrade)
- **#102** 🟢 Email/password fallback + TOTP MFA
- **#121** 🟢 OpenTelemetry → Grafana Cloud APM

## Active tech debt
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
*Updated 2026-06-22. Both PRs merged. Main is clean.*
