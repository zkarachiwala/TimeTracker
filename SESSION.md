# Session handoff — 2026-06-21

## Current state
- Branch: `feature/issue-159-showcase-css-sync` — ready to commit and raise PR

## Completed this session
- ✅ **#158 merged** (PR for #155 test framework migration)
- ✅ **#159 Showcase CSS sync + regression tests** — fully implemented:
  - Deleted `wwwroot-showcase/css/showcase-app.css`; MSBuild now copies `TimeTracker.Web/wwwroot/css/app.css` into showcase output (`TargetPath="wwwroot/css/app.css"`)
  - `wwwroot-showcase/index.html` updated to reference `css/app.css`
  - `ShowcaseFixture.cs` — publishes showcase via `dotnet publish -p:Showcase=true`, serves on port 7008 with `UsePathBase("/TimeTracker")`
  - `ShowcaseCollection.cs` — `[CollectionDefinition("Showcase")]`
  - `ShowcaseTests.cs` — 7 smoke tests covering all routed pages (timer, entries day, entries calendar, reports, projects, clients, admin users)
  - `<FrameworkReference Include="Microsoft.AspNetCore.App" />` added to `TimeTracker.Playwright.csproj` (needed for `WebApplication`)
  - `docs/playwright-strategy.md` renamed → `docs/testing-strategy.md` with 3 parts: Playwright E2E, Showcase, bUnit
  - `docs/decisions.md` — D027 added (showcase CSS unified via MSBuild)
  - `docs/roadmap.md` — Phase 13 added, dependency chain updated
  - `CLAUDE.md` updated — reference to `testing-strategy.md`, showcase test run command, Showcase Playwright section in Testing

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

## Next session
- Run the full test suite (user runs it) to verify showcase tests pass
- Merge PR for #159

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
*Updated 2026-06-21. Feature/issue-159-showcase-css-sync ready to commit and raise PR.*
