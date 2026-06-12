# Decision register

Architectural decisions that were non-obvious, had meaningful alternatives, or are likely to be revisited. Numbered permanently — never reuse an ID. When a decision is superseded, update its Status and add a forward reference.

| ID | Decision | Date | Status |
|----|----------|------|--------|
| [D001](#d001-global-wasm-rendering-over-ssr--wasm-islands) | Global WASM rendering over SSR + WASM islands | 2026-06 | Accepted |
| [D002](#d002-mudblazor-over-fluent-ui-blazor-and-bootstrap) | MudBlazor over Fluent UI Blazor and Bootstrap | 2026-05 | Accepted |
| [D003](#d003-zero-cost-hosting-azure-f1--azure-sql-free) | Zero-cost hosting: Azure F1 + Azure SQL free | 2026-05 | Accepted |
| [D004](#d004-vertical-slice-architecture-no-controllers-no-repositories) | Vertical Slice Architecture — no controllers, no repositories | 2026-05 | Accepted |
| [D005](#d005-cookie-based-auth-over-jwt) | Cookie-based auth over JWT | 2026-05 | Accepted |
| [D006](#d006-google-oauth-only--remove-usernamepassword-login) | Google OAuth only — remove username/password login | 2026-05 | Accepted |
| [D007](#d007-keep-rest-api-layer-separate-from-blazor-pages) | Keep REST API layer separate from Blazor pages | 2026-05 | Accepted |
| [D008](#d008-two-ef-core-dbcontexts-app--identity) | Two EF Core DbContexts — app + identity | 2026-05 | Accepted |
| [D009](#d009-mapster-over-automapper) | Mapster over AutoMapper | 2026-05 | Accepted |
| [D010](#d010-playwright-ci-auth--unauthenticated-tests-only-in-ci) | Playwright CI auth — unauthenticated tests only in CI | 2026-06 | Superseded by D016 |
| [D011](#d011-showcase--zero-changes-to-trackerclient) | Showcase — zero changes to TimeTracker.Client | 2026-06 | Accepted |
| [D012](#d012-showcase--in-memory-persistence) | Showcase — in-memory persistence only | 2026-06 | Accepted |
| [D013](#d013-showcase--demo-watermark-in-apprazor) | Showcase — demo watermark in App.razor | 2026-06 | Accepted |
| [D014](#d014-showcase--github-pages-deployment) | Showcase — GitHub Pages deployment | 2026-06 | Accepted |
| [D015](#d015-showcase-static-assets-isolated-to-wwwroot-showcase) | Showcase static assets isolated to `wwwroot-showcase/` | 2026-06 | Accepted |
| [D016](#d016-playwright--full-suite-pre-push-ci-smoke-test-only) | Playwright — full suite pre-push, CI smoke test only | 2026-06 | Accepted |

---

## D001: Global WASM rendering over SSR + WASM islands

**Date:** 2026-06 — **Status:** Accepted — **Tracks:** [TD1](technical-debt.md#infrastructure--compute), [TD6](technical-debt.md#cicd--testing)

**Context:** Phase 10 originally targeted a hybrid model: SSR router with WASM islands for interactive components. This was abandoned.

**Decision:** All routed pages run as global `InteractiveWebAssembly`. `TimeTracker.Web` is a shell (API + `App.razor` only). All pages and layouts live in `TimeTracker.Client`.

**Consequences:**
- ✅ Eliminates `RenderFragment` cross-boundary serialisation errors
- ✅ Single DI scope; no per-island prerender workarounds
- ✅ Offloads all UI compute to the browser — critical on Azure F1 (60 CPU-min/day)
- ✅ Full SPA routing in the browser; no server round-trips on navigation
- ❌ ~5 MB WASM bundle download on first visit
- ❌ No SEO (acceptable — app requires auth)
- ❌ MudBlazor SSR limitations become the permanent reason to stay on global WASM

**Full justification:** [architecture.md — Why global WASM, not SSR + WASM islands](architecture.md#why-global-wasm--not-ssr--wasm-islands)

---

## D002: MudBlazor over Fluent UI Blazor and Bootstrap

**Date:** 2026-05 (PR #38) — **Status:** Accepted — **Tracks:** [TD17](technical-debt.md#security)

**Context:** The app needed a full component library for a mobile-first UI (drawers, date pickers, data grids, dialogs, snackbars). Three options were evaluated.

**Decision:** MudBlazor. Material Design system, pure C# (no JS web component layer), 10k+ stars and monthly release cadence.

**Consequences:**
- ✅ Material Design — appropriate for a personal mobile app
- ✅ Pure C# — no JS runtime to debug
- ✅ Largest Blazor community component library by stars and contributors
- ❌ No SSR support (MudBlazor #9743) — locks in D001 as a permanent constraint
- ❌ Inline style injection requires `unsafe-inline` in CSP (TD17)
- ❌ Migration to Fluent UI Blazor estimated at 5–10 person-days if SSR ever becomes required

**Full justification:** [architecture.md — Why MudBlazor](architecture.md#why-mudblazor)

---

## D003: Zero-cost hosting — Azure F1 + Azure SQL free

**Date:** 2026-05 — **Status:** Accepted — **Tracks:** [TD1–TD5](technical-debt.md#infrastructure--compute)

**Context:** The app is a personal billing tool for a single developer. No commercial requirement to pay for hosting.

**Decision:** Azure App Service F1 (free tier) for compute, Azure SQL Database free offer for persistence. Hard constraint: zero ongoing cost.

**Consequences:**
- ✅ Zero monthly cost
- ❌ F1: 60 CPU-min/day limit, single instance, no deployment slots, auto-sleep
- ❌ Azure SQL free: serverless auto-pause (cold start latency), 7-day backup retention only
- ❌ No staging environment — drives TD3, TD6, TD7, D010 downstream

**Upgrade path:** Azure App Service Standard S1 (~$60/month) + Azure SQL Standard (~$20/month) removes the majority of constraints in the tech debt register.

---

## D004: Vertical Slice Architecture — no controllers, no repositories

**Date:** 2026-05 (PR #25) — **Status:** Accepted

**Context:** The app was migrated from a classic layered architecture (`TimeTracker.API` with controllers) to Blazor SSR + minimal APIs. The layering added indirection without benefit at this scale.

**Decision:** Vertical Slice Architecture. Each feature owns its service interface, implementation, DTOs, and endpoint registration. No shared controller base class, no repository abstraction over EF Core.

**Consequences:**
- ✅ Each feature is self-contained — adding or removing a feature touches one folder
- ✅ EF Core is the persistence layer; no leaky repository abstraction hiding its capabilities
- ✅ Minimal API endpoints are co-located with the service they call
- ❌ No repository layer means test isolation requires EF InMemory (used in `TimeTracker.Tests`)
- ❌ Cross-feature queries (e.g. reports) require direct context access or a dedicated service

---

## D005: Cookie-based auth over JWT

**Date:** 2026-05 — **Status:** Accepted

**Context:** Auth was redesigned for Google OAuth in PR #28. Two options for session management: HTTP-only cookies (ASP.NET Identity default) or JWT bearer tokens.

**Decision:** HTTP-only cookies via ASP.NET Identity's default cookie handler. `CookieCredentialHandler` in `TimeTracker.Client` sends `BrowserRequestCredentials.Include` so the cookie is forwarded with every WASM API request.

**Consequences:**
- ✅ Cookies are not accessible to JavaScript — XSS cannot steal session tokens
- ✅ ASP.NET Identity manages cookie issuance, renewal, and revocation hooks
- ✅ SameSite=Strict prevents CSRF from cross-origin requests
- ❌ Cookie is domain-scoped — Playwright auth state captured on `localhost` cannot be used against `timetracker-zak.azurewebsites.net` (the proximate cause of D010)
- ❌ No bearer token means the API cannot be called from a native mobile app or CLI without a browser cookie store

---

## D006: Google OAuth only — remove username/password login

**Date:** 2026-05 (PR #28) — **Status:** Accepted — **Tracks:** [TD10](technical-debt.md#authentication--authorisation)

**Context:** The original app had username/password registration and login backed by ASP.NET Identity. This was removed entirely in PR #28.

**Decision:** Google OAuth 2.0 only. No local credential store. `AllowedEmails` config list gates access.

**Consequences:**
- ✅ Eliminates password hashing, reset flows, account lockout, email verification — all complexity with no benefit for a single-user app
- ✅ Google handles credential security (MFA, breach detection, account recovery)
- ❌ Full dependency on Google OAuth availability — no fallback if Google is unreachable
- ❌ No path to enterprise SSO (SAML, Entra ID) without adding a second provider
- ❌ `AllowedEmails` list requires a redeploy to add a new user (TD9)

---

## D007: Keep REST API layer separate from Blazor pages

**Date:** 2026-05 — **Status:** Accepted

**Context:** All data access could have been implemented directly in Blazor SSR pages (server-side EF calls). Instead, a REST API layer was retained with explicit endpoints and service interfaces.

**Decision:** Feature services (`ITimeEntryService`, `IProjectService`, `IClientService`) are exposed via minimal API endpoints (`MapTimeEntryEndpoints()` etc.) consumed by `TimeTracker.Client` HTTP services over HTTP/HTTPS.

**Consequences:**
- ✅ Required for global WASM (D001) — WASM has no direct server access
- ✅ REST API is available for future automation (Zoho Books invoice export — see [roadmap](roadmap.md))
- ✅ Service interfaces can be mocked for `TimeTracker.Showcase` (Phase 11) without any changes to the API layer
- ❌ Extra HTTP round-trip for every data operation vs. direct EF calls in server components
- ❌ Doubles the code surface for each feature (server endpoint + client HTTP service)

---

## D008: Two EF Core DbContexts — app + identity

**Date:** 2026-05 — **Status:** Accepted

**Context:** ASP.NET Identity has its own schema requirements. It can share a single DbContext or use a dedicated one.

**Decision:** Two DbContexts targeting the same SQL Server database (`TimeTrackerDb`): `TimeTrackerDataContext` (`app` schema) for business entities, `IdentityDataContext` (`id` schema) for ASP.NET Identity tables.

**Consequences:**
- ✅ Clean schema separation — Identity schema is owned by the framework, app schema is owned by the team
- ✅ EF migrations for app data and identity are fully independent — no cross-contamination
- ✅ `TimeTrackerDataContext` never touches Identity tables and vice versa
- ❌ Two connection strings to manage (identical credentials, different logical ownership)
- ❌ Cannot write a single EF query that joins app entities with Identity users — requires two context calls

---

## D009: Mapster over AutoMapper

**Date:** 2026-05 — **Status:** Accepted

**Context:** Entity ↔ DTO mapping was needed for all features. AutoMapper is the traditional choice; Mapster is the performance-oriented alternative.

**Decision:** Mapster with per-feature `IRegister` classes scanned at startup via `TypeAdapterConfig.GlobalSettings.Scan()`.

**Consequences:**
- ✅ Mapster benchmarks show significantly lower allocation overhead than AutoMapper at mapping time
- ✅ `IRegister` pattern co-locates mapping configuration with the feature it belongs to — consistent with Vertical Slice (D004)
- ✅ No dependency on AutoMapper's profile registration ceremony
- ❌ Mapster is less widely known — developers familiar with AutoMapper need to learn the API
- ❌ Fewer StackOverflow answers and community examples than AutoMapper

---

## D010: Playwright CI auth — unauthenticated tests only in CI

**Date:** 2026-06 — **Status:** Superseded by [D016](#d016-playwright-full-suite-pre-push-ci-smoke-test-only) — **Tracks:** [TD6](technical-debt.md#cicd--testing), [TD7](technical-debt.md#cicd--testing)

**Context:** The app uses Google OAuth. The OAuth flow cannot be automated in CI. Auth state captured locally against `localhost` uses domain-scoped cookies that are never sent to `timetracker-zak.azurewebsites.net`. All 29 authenticated Playwright tests were failing in CI.

**Decision:** Authenticated Playwright tests are excluded from CI. CI runs only the 9 unauthenticated tests. Authenticated tests run locally after `CaptureAuthState` is executed against `https://localhost:7006`.

**Implementation:**
- `AuthenticatedPageTest` and `AuthenticatedDesktopPageTest` carry `[Category("Authenticated")]`
- A `[OneTimeSetUp]` guard calls `Assert.Ignore()` when no auth state file is present — tests are skipped (not failed) in CI
- The pre-push hook (`.githooks/pre-push`) runs unauthenticated tests on push when app code changes

**Options considered and rejected:**

| Option | Why rejected |
|--------|-------------|
| Token-gated bypass endpoint in production (always-on, runtime-checked) | Privileged auth bypass in a production app handling real billing data — assessed as failing a security audit |
| Toggle token via Azure app settings per CI run | Two app restarts per deploy pipeline; same privileged endpoint concern |
| Commit localhost auth state as a GitHub secret | Cookie `domain: localhost` is never sent to `timetracker-zak.azurewebsites.net` — was the previous broken approach |

**Proper resolution:** Staging environment (D003 / TD3) with a dedicated Google OAuth app registration. Authenticated tests run against staging in CI.

---

## D011: Showcase — zero changes to TimeTracker.Client

**Date:** 2026-06 — **Status:** Accepted — **Phase:** 11 (not yet built)

**Context:** `TimeTracker.Showcase` needs to render all existing WASM pages as a portfolio demo. The question was whether to modify `TimeTracker.Client` to accommodate showcase-specific requirements.

**Decision:** `TimeTracker.Client` is used as a read-only dependency. No modifications to any page, layout, or component in that project. The showcase's own `Program.cs` registers mock DI implementations instead of the HTTP services.

**Consequences:**
- ✅ `TimeTracker.Client` never needs to know the showcase exists — zero ongoing coupling
- ✅ Showcase may break when Client changes, but Client is never constrained by showcase needs
- ✅ `MockAuthenticationStateProvider` returns a hardcoded "Demo User" — satisfies all `[Authorize]` attributes without a login flow
- ❌ `TimeTracker.Client` uses `Microsoft.NET.Sdk.BlazorWebAssembly` (a runnable app SDK, not a class library SDK) — referencing one WASM app from another is non-standard; if build tooling rejects it at implementation time this decision must be revisited

---

## D012: Showcase — in-memory persistence

**Date:** 2026-06 — **Status:** Accepted — **Phase:** 11 (not yet built)

**Context:** Mock services need some form of persistence. Three options were evaluated.

| Option | Complexity | Refresh persistence | Offline PWA foundation |
|--------|-----------|--------------------|-----------------------|
| **A — In-memory** | None | ❌ Resets | ❌ Would require rewrite |
| B — localStorage | Low | ✅ Survives | Partial (size-limited, sync) |
| C — IndexedDB/SQLite | Medium–high | ✅ Survives | ✅ Right foundation |

**Decision:** Option A — in-memory only. Data resets on browser refresh.

**Consequences:**
- ✅ Zero complexity — mock services are plain C# classes with no storage dependency
- ✅ Acceptable for a portfolio demo — visitors explore, not manage real data
- ❌ Refresh resets all demo data — acknowledged and accepted
- ❌ Not a foundation for offline PWA — if that becomes a goal, the mock store requires a rewrite

---

## D013: Showcase — demo watermark in App.razor

**Date:** 2026-06 — **Status:** Accepted — **Phase:** 11 (not yet built)

**Context:** Showcase visitors need to know they are using mock data, not the live system.

**Decision:** A "Demo Mode" banner is rendered above the `<Routes />` outlet in `TimeTracker.Showcase`'s own `App.razor`. Nothing in `TimeTracker.Client` is modified (see D011).

**Consequences:**
- ✅ Banner is entirely outside `Client` — zero regression risk to the production app
- ✅ Impossible to miss — renders on every page

---

## D015: Showcase static assets isolated to `wwwroot-showcase/`

**Date:** 2026-06 — **Status:** Accepted — **Phase:** 11 — **Tracks:** [TD22](technical-debt.md#cicd--testing)

**Context:** The showcase SPA hosting files (`index.html`, `404.html`, `showcase-app.css`) were initially placed in `TimeTracker.Client/wwwroot/`. Because `TimeTracker.Web` references `TimeTracker.Client`, the Blazor static web asset pipeline merges all `wwwroot/` content from referenced projects into the host's serving root. Any change to these files updated asset fingerprints (`blazor.boot.json`), staling the running dev server and causing spurious pre-push Playwright failures.

**Options considered:**

| Option | Prevents dev-server fingerprint churn | Excluded from hosted-mode publish |
|--------|--------------------------------------|----------------------------------|
| `CopyToPublishDirectory="Never"` | ❌ Still fingerprinted at build time | ✅ Excluded from publish only |
| `Content Remove` behind MSBuild condition | ✅ Files invisible to SDK in normal builds | ✅ |
| Separate `wwwroot-showcase/` folder + conditional mapping | ✅ Default glob never captures them | ✅ |

**Decision:** Move all showcase hosting files to `TimeTracker.Client/wwwroot-showcase/`. A conditional `ItemGroup` in the csproj maps them into the publish output only when `-p:Showcase=true` is passed. `#if SHOWCASE` (C# preprocessor) continues to gate the mock DI registrations — both flags are passed together in CI: `-p:Showcase=true -p:DefineConstants=SHOWCASE`.

**Consequences:**
- ✅ Showcase file changes never touch the `wwwroot/` asset pipeline — no fingerprint churn, no dev-server restarts
- ✅ Intent is explicit: `wwwroot-showcase/` is visually distinct from the real app's `wwwroot/`
- ✅ Default content glob never accidentally picks up showcase files
- ❌ Two MSBuild flags required for showcase publish (`Showcase=true` + `DefineConstants=SHOWCASE`)
- ❌ Requires researcher verification: `TargetPath` on `Content` items must correctly map into `wwwroot/` in the publish output — verified against SDK source (`DefineStaticWebAssets.cs`)

---

## D014: Showcase — GitHub Pages deployment

**Date:** 2026-06 — **Status:** Accepted — **Phase:** 11 (not yet built)

**Context:** The showcase is static WASM — it needs a static file host. GitHub Pages is free and the repository is already public.

**Decision:** Deploy to `zkarachiwala.github.io/TimeTracker` via a `showcase` job in `deploy.yml`, publishing to the `gh-pages` branch. Base href `/TimeTracker/` set at publish time. `index.html` copied to `404.html` for SPA routing.

**Constraints verified at decision time:**
- GitHub Pages requires a public repository on a free personal account — `TimeTracker` is already public ✓
- 1 GB size limit, 100 GB/month bandwidth — no risk for a portfolio app ✓
- Source: [GitHub Pages limits](https://docs.github.com/en/pages/getting-started-with-github-pages/github-pages-limits)

**Consequences:**
- ✅ Zero cost, no additional infrastructure
- ✅ `showcase` job is fully isolated — no Azure credentials, cannot affect the production deploy
- ❌ GitHub Pages does not support custom headers or server-side redirects — SPA routing workaround (`404.html`) is required
- ❌ Sub-path deployment (`/TimeTracker/`) requires correct `<base href>` — must be set at publish time, not hardcoded

---

## D016: Playwright — full suite pre-push, CI smoke test only

**Date:** 2026-06 — **Status:** Accepted — **Supersedes:** [D010](#d010-playwright-ci-auth--unauthenticated-tests-only-in-ci) — **Tracks:** [TD6](technical-debt.md#cicd--testing)

**Context:** D010's approach (unauthenticated tests only in CI, auth state captured manually) broke immediately — the 1-day cookie TTL meant the stored GitHub secret expired overnight, the `PLAYWRIGHT_AUTH_STATE_B64` secret had to be recaptured manually before every CI run, and all authenticated tests were silently skipped in CI regardless. There is no staging environment (D003); the only ASP.NET Core `Development` environment is `localhost`.

**Decision:** Full authenticated Playwright suite runs locally via the pre-push hook. CI runs a smoke test only — curl the production login page and assert HTTP 200. This is the correct split given the infrastructure constraints.

**How it works:**
- `GlobalSetup.cs` (`[SetUpFixture]`) starts the app automatically if not running, then calls `/api/dev/login` (a Development-only endpoint) via Playwright's `APIRequestContext` to obtain a fresh auth cookie. `dotnet test` is the only command needed — no manual steps, no pre-generated tokens.
- The pre-push hook runs the full suite when app code changes. Doc-only or test-only pushes skip it.
- CI (`deploy.yml` `smoke` job) curls `https://timetracker-zak.azurewebsites.net/login` after deploy and fails the pipeline if it does not return HTTP 200.

**Why not run authenticated tests in CI:**
- `/api/dev/login` is only available when `ASPNETCORE_ENVIRONMENT=Development` — it is not present in production
- Running a Development-mode app in CI just for Playwright would require a separate service container with a database, migrations, seeded admin user, and Google OAuth configuration — that is a staging environment
- The smoke test is sufficient to confirm the deploy succeeded; functional regression is caught by the pre-push hook before the code ever reaches CI

**If a staging environment is added (resolves TD3):**
1. Deploy the staging app with `ASPNETCORE_ENVIRONMENT=Development` and a seeded admin user
2. Point `PLAYWRIGHT_BASE_URL` at the staging URL in the CI workflow
3. The `GlobalSetup` will call `/api/dev/login` on staging, capture auth, and run the full suite in CI
4. The pre-push hook can be removed or kept as a fast local gate
5. Remove the `smoke` job and replace with `dotnet test TimeTracker.Playwright` in the CI pipeline

**Consequences:**
- ✅ `dotnet test` is fully automated — no manual token capture, no expiring secrets
- ✅ CI is reliable — smoke test cannot expire or fail due to auth state rot
- ✅ `PLAYWRIGHT_AUTH_STATE_B64` GitHub secret can be deleted
- ❌ Authenticated regression tests do not run in CI — a broken authenticated page can reach production if the developer skips the pre-push hook (e.g. `git push --no-verify`)
- ❌ The pre-push hook adds ~90 seconds to every push that touches app code (app startup + test run)
