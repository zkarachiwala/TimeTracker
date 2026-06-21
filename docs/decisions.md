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
| [D017](#d017-cloudflare-free-plan-over-paid-cdnwaf) | Cloudflare free plan over paid CDN/WAF | 2026-06 | Accepted |
| [D018](#d018-defence-in-depth-for-api-query-abuse--pagination-cap--global-rate-limiting--cancellation-tokens) | Defence-in-depth for API query abuse — pagination cap + global rate limiting + cancellation tokens | 2026-06 | Accepted |
| [D019](#d019-serilog--health-endpoint--uptimerobot-over-application-insights) | Serilog + /health endpoint + UptimeRobot over Application Insights | 2026-06 | Accepted |
| [D020](#d020-sql-server-row-level-security--audit-trail) | SQL Server Row-Level Security + audit trail | 2026-06 | Accepted |
| [D021](#d021-nightly-bacpac-export-to-private-github-repo) | Nightly `.bacpac` export to private GitHub repo | 2026-06 | Accepted |
| [D022](#d022-ef-core-migrateAsync-at-startup) | EF Core `MigrateAsync()` at startup | 2026-06 | Accepted |
| [D025](#d025-publicholiday-for-au-public-holiday-resolution) | `PublicHoliday` for AU public holiday resolution | 2026-06 | Accepted |
| [D026](#d026-xunit-over-nunit-for-playwright--new-bunit-component-layer) | xUnit over NUnit for Playwright + new bUnit component layer | 2026-06 | Accepted |

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
- ❌ Inline style injection requires `unsafe-inline` in CSP `style-src` — this is a real security gap. Without it MudBlazor's runtime styling breaks; with it, CSS injection attacks (e.g. attribute-selector exfiltration of sensitive values via `input[value^="a"] { background: url(...) }`) cannot be blocked by CSP. See [TD17](technical-debt.md#security).
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

---

## D018: Defence-in-depth for API query abuse — pagination cap + global rate limiting + cancellation tokens

**Date:** 2026-06 — **Status:** Accepted

**Context:** Three compounding vulnerabilities were identified in the API layer:

1. The `limit` parameter on paginated endpoints is caller-controlled with no server-side maximum — a caller can request an arbitrarily large result set in a single request.
2. Two unbounded `/all` endpoints (`/year/{year}/all`, `/project/{projectId}/all`) exist to serve the reports page and have no row limit at all.
3. No cancellation tokens are used — if a client disconnects mid-request, the server continues executing EF Core queries and holding SQL connections to completion.
4. Rate limiting was applied to auth endpoints only; all data endpoints were unprotected.

A stolen cookie combined with rapid-fire requests to the unbounded endpoints would exhaust the Azure SQL free-tier connection pool (75-connection limit) with no circuit breaker.

**Decision:** Three independent layers, implemented in order:

| Layer | What it does | Issue |
|-------|-------------|-------|
| Server-side pagination cap | `Math.Min(limit, 200)` in `ToWrapper` — caps cost of any single paginated request | #116 |
| Global rate limiting | Default policy (120 req/min) covers all endpoints; tighter explicit policy (10 req/min) on `/all` endpoints | #100 |
| Cancellation tokens | `CancellationToken` threaded through all service methods and EF Core calls — aborted requests release SQL connections immediately | #115 |

Each layer is independently valuable but they work together: the cap limits per-request cost, rate limiting limits request frequency, and cancellation tokens ensure abandoned requests don't linger.

**Why not just rate limiting:** Rate limiting controls frequency but not the cost of a single request that gets through. A 120/min limit still allows 120 expensive unbounded queries per minute.

**Why not just cancellation tokens:** Cancellation tokens release resources on disconnect but don't prevent an attacker who stays connected from hammering the endpoint.

**Why global default over per-endpoint decoration for rate limiting:** The endpoint count may grow; a global default ensures new endpoints are covered automatically. Named policies on the `/all` endpoints and auth endpoints override the default where a tighter limit is needed.

**Consequences:**
- ✅ Layered protection — no single missing piece leaves the app fully exposed
- ✅ Each layer is independently useful and teaches a transferable pattern
- ✅ Global rate limiting default prevents accidentally unprotected new endpoints
- ❌ `Math.Min(limit, 200)` silently truncates large requests — callers receive fewer records than requested with no error. Acceptable for a personal app; a public API would return 400 Bad Request instead.
- ❌ Cancellation tokens require threading through all service method signatures — largest refactor of the three

---

## D017: Cloudflare free plan over paid CDN/WAF

**Date:** 2026-06 — **Status:** Superseded — **Tracks:** [TD21](technical-debt.md#cdn--networking)

**Context:** This decision was based on the incorrect assumption that a custom domain (`timetracker.dzk.com.au`) could be pointed at the app via Cloudflare. Two blockers make this unworkable at zero cost: (1) Azure App Service F1 free tier does not natively support custom domains, and (2) Cloudflare free tier URL redirections do not function as needed. The production URL has always been `https://timetracker-zak.azurewebsites.net`.

**Decision:** No CDN/WAF layer. The app is served directly from Azure App Service at `timetracker-zak.azurewebsites.net`. Azure provides TLS termination via the default `*.azurewebsites.net` certificate at no cost.

**Consequences:**
- ✅ Zero cost — consistent with D003 zero-cost hosting constraint
- ❌ No CDN caching or DDoS mitigation
- ❌ No WAF
- ❌ No custom domain

**Upgrade path:** A custom domain and CDN/WAF (Cloudflare or Azure Front Door) become available by upgrading App Service to at least the Basic (B1) tier.

---

## D019: Serilog + /health endpoint + UptimeRobot over Application Insights

**Date:** 2026-06 — **Status:** Accepted — **Tracks:** [TD23](technical-debt.md#observability)

**Context:** #97 required structured logging, a health check endpoint, and uptime monitoring. Application Insights is the natural Azure APM choice, but the current workspace-based model stores data in Log Analytics with pay-as-you-go pricing — no free monthly data allowance. Free hosting is a hard constraint ([D003](#d003-zero-cost-hosting-azure-f1--azure-sql-free)).

**Options considered:**

| Option | Cost | Structured logs | Distributed tracing | Uptime monitoring |
|--------|------|----------------|---------------------|-------------------|
| **Serilog → console + UptimeRobot** | $0 | ✅ JSON stdout | ❌ | ✅ (free tier) |
| Application Insights | Pay-as-you-go per GB | ✅ | ✅ | ✅ |
| OpenTelemetry → Grafana Cloud | $0 (free tier) | ✅ | ✅ | ✅ |

**Decision:** Serilog with structured JSON console output (Azure App Service captures stdout and makes it queryable in the portal), a `/health` endpoint backed by EF Core DB connectivity checks, and UptimeRobot (free tier) monitoring `/health` externally. Application Insights deferred due to cost. OpenTelemetry → Grafana Cloud tracked as the future path in [#121](https://github.com/zkarachiwala/TimeTracker/issues/121).

**External monitoring service — options considered:**

All four services have a genuine free tier. Cost was therefore not the differentiator; the decision turned on maturity, interval, and feature set.

| Service | Free check interval | Free monitors | Established | Notes |
|---------|---------------------|---------------|-------------|-------|
| **UptimeRobot** | 5 min | 50 | 2010 | Industry default; status pages included on free tier |
| Freshping | 1 min | 50 | 2017 | Faster interval; Freshworks product (vendor lock-in risk) |
| Better Stack | 3 min | 10 | 2021 | Better incident management UI; newer, fewer references |
| StatusCake | 5 min | Unlimited | 2012 | More monitors but fewer community references |

**Why UptimeRobot:** Longest-established free monitoring service (15+ years), most community documentation, status pages included on the free tier, and 50 monitors is more than sufficient. The 5-minute interval is the free-tier minimum for all comparable services except Freshping. Freshping's 1-minute interval is better but Freshworks is a large SaaS vendor adding a dependency that could change the free tier terms; UptimeRobot's free tier has been stable for many years.

**Consequences:**
- ✅ Zero cost — structured logs available in Azure App Service log stream and deployments blade
- ✅ `/health` endpoint enables external monitoring and gives a reliable readiness signal
- ✅ Serilog is the .NET industry standard — high transferable skill value
- ❌ No distributed tracing or correlation IDs — individual requests cannot be traced end-to-end
- ❌ No performance baselines or alerting on error spikes (UptimeRobot only alerts on downtime, not degradation)
- ❌ Log retention is limited to what Azure App Service log stream keeps (short-lived); no queryable log store

---

## D020: SQL Server Row-Level Security + audit trail

**Date:** 2026-06 — **Status:** Accepted — **Supersedes:** [TD19](technical-debt.md#security)

**Context:** Data isolation was enforced only at the service layer (`IUserContextService`). A bug in the service layer, or direct `DbContext` access bypassing the service, could expose cross-user data. SQL Server Row-Level Security (RLS) is available on the free Azure SQL tier at zero cost.

**Decision:** Add RLS filter predicates to `TimeEntries`, `ProjectUsers`, and `Projects`. Add audit trail columns (`CreatedBy`, `UpdatedBy`, `DeletedBy`) to all entities via `SaveChangesAsync` override in `TimeTrackerDataContext`.

**How it works:**

- `UserSessionContextInterceptor` (`DbCommandInterceptor`) sets `SESSION_CONTEXT(N'UserId')` before every EF Core command. This fires per-command (not per-connection) because SQL Server connection pooling reuses physical connections without resetting session context.
- Three SQL Server predicate functions in the `app` schema:
  - `fn_filter_by_user_id` — reused for `TimeEntries` and `ProjectUsers` (direct `UserId` column)
  - `fn_filter_projects_by_user` — EXISTS join into `ProjectUsers` (Projects have no direct `UserId`)
- `SECURITY POLICY` on each table with `STATE = ON`
- Audit columns (`CreatedBy`, `UpdatedBy`, `DeletedBy`) populated in `TimeTrackerDataContext.SaveChangesAsync` from `IHttpContextAccessor`

**db_owner bypass (intentional):**

SQL Server exempts `db_owner` and `sysadmin` members from RLS by design. Local development uses `sa` (sysadmin), so policies are bypassed locally. Production uses Managed Identity with `db_datareader + db_datawriter` only — RLS is fully enforced there. This is the correct split: local dev has unrestricted access; production is locked down.

**Permission requirements for migration:**

The EF Core migration creates SQL functions and security policies. The app DB user requires two one-time grants (see `azure-deployment.md` Step 5b):
```sql
GRANT CREATE FUNCTION TO [timetracker-zak];
GRANT ALTER ANY SECURITY POLICY TO [timetracker-zak];
```
These are not in `db_datareader`/`db_datawriter`. Without them the migration fails at the RLS step.

**Local RLS verification:**


Because `sa` bypasses RLS, cross-tenant isolation cannot be verified locally with the default dev login. `RlsIntegrationTests` in `TimeTracker.Tests` provides three tests that:
1. Seed data via an admin (`sa`) connection
2. Create a temporary `timetracker_rls_test` login with `db_datareader + db_datawriter` only
3. Assert that querying as that login with `SESSION_CONTEXT` set to User A returns only User A's rows

Enable with: `SQL_SERVER_RLS_TESTS=true SQL_SERVER_ADMIN_CONNECTION=... SQL_SERVER_APP_CONNECTION=...`

**Clients not included:**

`Clients` has no direct `UserId` column and is linked to users via Projects → ProjectUsers (two hops). The service layer already scopes client access through projects. Applying RLS to `Clients` requires a multi-hop predicate that adds complexity with low additional security benefit at current scale. Deferred.

**Consequences:**

- ✅ Defence in depth — data isolation holds even if `IUserContextService` is bypassed
- ✅ Audit trail on all entities — who created, updated, and soft-deleted each record
- ✅ RLS is a standard enterprise pattern — high transferable skill value
- ✅ Zero cost — Azure SQL free tier supports RLS natively
- ❌ One extra `sp_set_session_context` round-trip per EF Core command (~0.1ms on localhost; negligible on Azure SQL in the same region)
- ❌ `sa` bypass means local dev cannot directly observe RLS filtering — requires opt-in integration tests
- ❌ `Clients` table not covered — service-layer isolation remains the only guard there

---

## D021: Nightly `.bacpac` export to private GitHub repo

**Date:** 2026-06 — **Status:** Accepted — **Closes:** [#104](https://github.com/zkarachiwala/TimeTracker/issues/104)

**Context:** Azure SQL free tier provides only 7-day automated backup retention (see [D003](#d003-zero-cost-hosting-azure-f1--azure-sql-free), [TD4](technical-debt.md#infrastructure--compute)). A longer-lived export was needed. Free hosting is a hard constraint.

**Options considered:**

| Option | Cost | Private by default | Notes |
|--------|------|--------------------|-------|
| **GitHub Actions → private repo** | $0 | ✅ | Fine-grained PAT scoped to one repo; no extra infrastructure |
| GitHub Actions → artifact | $0 | ❌ | Artifacts on public repos are publicly downloadable by anyone — not safe for personal data |
| Azure Blob Storage | ~$0.02/GB/month | ✅ | No true free tier; requires Storage Account and SAS token management |
| Azure SQL long-term retention | Paid tier only | ✅ | Not available on the free offer |

**Decision:** Export a `.bacpac` nightly via GitHub Actions to a dedicated private repository (`TimeTracker-backups`). Keep a rolling 30-day window; files older than 30 days are deleted before each commit.

**Credential model — why this specific setup:**

- **OIDC instead of a client secret** — the deploy SP already uses OIDC (no stored secrets); consistency and no secret rotation burden.
- **Dedicated SP (`timetracker-github-backup`)** — separate from the deploy SP so that if the backup credential is ever compromised it cannot deploy code.
- **Custom Azure RBAC role (firewall write/delete only)** — the SP needs to open and close a SQL firewall rule for the runner's ephemeral IP. The minimum built-in role that covers this (`SQL Server Contributor`) also grants the ability to modify the SQL server itself. A custom role with only `firewallRules/write` + `firewallRules/delete`, scoped to the SQL server resource, is the correct minimum.
- **`db_owner` on the database** — two constraints make this the minimum viable SQL permission: (1) SqlPackage requires `DBCC SHOW_STATISTICS` to analyse indexes, which requires `db_owner` or `db_ddladmin`; `db_datareader` alone is insufficient. (2) RLS filter policies block all rows for a connection with no `SESSION_CONTEXT` — a `db_datareader` account would export empty tables. `db_owner` is exempt from RLS by SQL Server design. The Azure RBAC role above limits what the SP can do to Azure infrastructure; `db_owner` on the database does not expand that.
- **Fine-grained PAT for the repo push** — OIDC cannot push to GitHub; a PAT is required. A fine-grained PAT scoped to `TimeTracker-backups` with `contents: write` only is the minimum possible credential. It cannot read or write any other repository.
- **Private repo over artifact** — GitHub Actions artifacts on public repositories are downloadable by any unauthenticated user. A `.bacpac` contains all user data; a private repository is the correct boundary.

**Consequences:**
- ✅ Zero cost — GitHub Actions minutes and private repos are free under the personal plan
- ✅ No stored client secrets anywhere — OIDC for Azure, fine-grained PAT for GitHub
- ✅ Backup credential is isolated — cannot deploy code or access any Azure resource other than firewall rules on one SQL server
- ✅ 30-day rolling retention is managed automatically — no manual cleanup
- ❌ Fine-grained PATs expire (maximum 1 year) — requires a calendar reminder to rotate `BACKUP_REPO_TOKEN` annually
- ❌ `db_owner` on the database is broader than ideal — necessary given SqlPackage and RLS constraints, but worth reviewing if either changes in a future SqlPackage version

---

## D022: EF Core `MigrateAsync()` at startup

**Date:** 2026-06 — **Status:** Accepted

**Context:** EF Core migrations need to reach the production database whenever schema changes are deployed. Options are: run migrations manually, add a migration step to the CI/CD pipeline, or call `MigrateAsync()` in `Program.cs` so the app migrates itself on startup.

**Decision:** Call `Database.MigrateAsync()` for both `TimeTrackerDataContext` and `IdentityDataContext` during app startup in `Program.cs`.

**Options considered:**

| Option | Effort | Risk |
|--------|--------|------|
| **`MigrateAsync()` at startup** | Zero — already in place | Race condition on multi-instance deploy |
| CI/CD pipeline step (`dotnet ef database update`) | Low — one workflow step | Requires prod connection string as CI secret |
| Manual (`dotnet ef database update` or SQL script) | High — manual step on every deploy | Human error; easy to forget |

**Consequences:**
- ✅ Zero operational overhead — migrations are automatic on every deploy
- ✅ No secrets needed in CI — the app already has DB credentials via Azure App Service config
- ✅ Correct for single-instance hosting (Azure F1 runs one instance at a time)
- ❌ If two instances start simultaneously (e.g. during a slot swap or scale-out), they can race on the same migration — one will fail with a constraint violation
- ❌ App startup is blocked until migrations complete — acceptable for a personal app, wrong for high-availability services

**Why the trade-off is acceptable:** Azure F1 is single-instance by definition. There is no slot swap or scale-out. The race condition cannot occur at current hosting tier. If the app ever moves to a paid tier with multiple instances, this decision should be revisited and replaced with a pipeline migration step.

---

## D023: Single-tenant architecture — one company, shared data

**Date:** 2026-06 — **Status:** Accepted

**Context:** The system is built for a single company whose employees are the users. All users within that company share the same projects, clients, and time-entry data. The question arose whether multi-tenancy (company-level segregation) should be introduced as part of adding user management (#95).

**Decision:** The system remains explicitly single-tenant. There is no `Company` or `Tenant` entity. All authenticated users operate within the same data space.

**Multi-tenancy is explicitly out of scope.** If the system were ever extended to serve multiple companies it would require:
- A `Company` (tenant) entity with a global admin who bootstraps each tenant
- Foreign keys from every table to `CompanyId`
- Row-level security or query filters on every query
- Separate OAuth app registrations per tenant
- A complete redesign of the bootstrap and onboarding flow

This is a foundational change, not an incremental one. It is not justified for the current use case.

**What this means for the current design:**
- Projects and clients are not confidential between colleagues — all authenticated users can see all projects and clients
- `ProjectUser` controls who can *log time* against a project, not who can *see* it
- Reports and time entries remain user-scoped (you see your own time only)
- User management (adding users, assigning roles) is handled by the Admin UI — see [D024](#d024-projectuser-as-time-allocation-gate--orphaned-reference-pattern)

**Consequences:**
- ✅ Zero complexity overhead — no tenant ID on any query
- ✅ Simple user management — one admin, one user table, one set of projects
- ❌ Cannot serve multiple companies from the same instance — a second company would see the first company's data

---

## D024: `ProjectUser` as time-allocation gate + orphaned reference pattern

**Date:** 2026-06 — **Status:** Accepted — **Tracks:** [#95](https://github.com/zkarachiwala/TimeTracker/issues/95)

**Context:** Adding multi-user support required deciding what `ProjectUser` membership controls. Two options: (a) gate *visibility* — only see projects you're assigned to; (b) gate *time allocation* — only log time against projects you're assigned to, but see all projects.

**Decision:** `ProjectUser` is a time-allocation gate only. All authenticated users can see all projects and clients. Only the time-entry project dropdown is restricted to assigned projects.

**Rationale:**
- Projects and clients are not confidential between colleagues (see [D023](#d023-single-tenant-architecture--one-company-shared-data))
- A user removed from a project must still be able to see their historical entries and navigate to the project — hiding it would create unexplained gaps in their records
- Reports must reconcile — YTD totals and project breakdowns must account for all the user's entries including those on projects they are no longer assigned to

**Orphaned reference pattern for dropdowns:**

When a user has a time entry referencing a project they are no longer assigned to, that entry's project is an *orphaned reference* — valid historical data pointing to something no longer selectable. The dropdown handles this as follows:

| Scenario | Dropdown contents | Behaviour |
|----------|-------------------|-----------|
| New entry | Assigned projects only | All items selectable |
| Editing entry, project still assigned | Assigned projects only | All items selectable; can freely change project |
| Editing entry, project no longer assigned | Assigned projects + current project (disabled, labelled *"(not assigned)"*) | Can change to any assigned project; orphaned project is visible but not selectable |

This pattern — showing a disabled placeholder for the current value when the referenced item is no longer available — prevents blank or broken fields while preserving the constraint. It is used widely in SaaS tools (Jira archived sprints, Xero deleted accounts, GitHub removed labels). See [UI Patterns — Orphaned reference in dropdowns](architecture.md#orphaned-reference-in-dropdowns) for the implementation pattern.

**`GetAssignedProjects` vs `GetAllProjects`:**
- `GetAllProjects` — returns all non-deleted projects; used by project list page, project detail page, and reports
- `GetAssignedProjects` — returns only projects where `ProjectUser.UserId == currentUser`; used exclusively by the time-entry form project dropdown

**Consequences:**
- ✅ Historical entries always visible and correctly labelled
- ✅ Reports reconcile — all user entries included regardless of current assignment
- ✅ Clear, auditable gate — project assignment is about time allocation, not data access
- ❌ A removed user can still navigate to the project detail page and see their own entries there — this is intentional and correct

---

## D025: `PublicHoliday` for AU public holiday resolution

**Date:** 2026-06 — **Status:** Accepted — **Closes:** [#137](https://github.com/zkarachiwala/TimeTracker/issues/137) — **Tracks:** [TD25](technical-debt.md#business-rules)

**Context:** Issue #137 introduced award rates — a secondary billing rate applied automatically on weekends and public holidays. Weekend detection is trivial (`DayOfWeek`). Public holiday detection requires a data source.

**Options considered:**

| Option | Pros | Cons |
|--------|------|------|
| **`PublicHoliday` NuGet (MIT)** | Free, open-source, no key, offline, AU + state support, 6.6M downloads, actively maintained | NuGet dependency |
| `Nager.Date` NuGet | Well-known, AU support | **Rejected** — v2.22 requires a GitHub sponsor license key; throws `LicenseKeyException` at runtime without one. Not free. |
| Hardcoded DB seed table | Full control, editable | Manual update each year, migration required |
| Public holiday API | Always current | External dependency, rate limits, potential cost |

**Decision:** Use [`PublicHoliday`](https://github.com/martinjw/holiday) by martinjw (MIT licence, v3.13.0). Holiday resolution is a pure in-process call via `new AustraliaPublicHoliday().IsPublicHoliday(date)` — no network hop, no API key, no DB table to maintain. National AU holidays only for the initial implementation.

**Scope limitation:** Only national Australian public holidays are resolved at launch. State/territory holidays are supported by the library (`AustraliaPublicHoliday` accepts a state enum) but not enabled until the jurisdiction question is resolved — see [TD25](technical-debt.md#business-rules).

**Consequences:**
- ✅ Zero cost — MIT-licensed, resolves offline
- ✅ No DB schema or migration for holiday data
- ✅ Covers the core use case (national AU holidays) immediately
- ✅ State support is built in — enabling it requires only passing a state enum once TD25 is resolved
- ❌ State-level holidays not covered until TD25 is resolved
- ❌ Adds one NuGet dependency to `TimeTracker.Web`

---

## D026: xUnit over NUnit for Playwright + new bUnit component layer

**Date:** 2026-06 — **Status:** Accepted — **Issue:** [#155](https://github.com/zkarachiwala/TimeTracker/issues/155)

**Context:** `TimeTracker.Playwright` was written in NUnit (the original Phase 9 choice). During the #146 nav rail work, a teardown hang was diagnosed: Playwright NUnit's `WorkerAwareTest.WorkerTeardown` calls `Browser.CloseAsync()` with no timeout. When a CDP operation leaves the connection in a stuck state, the whole test process hangs indefinitely. The fix — per-test `Page.CloseAsync().WaitAsync(10s)` in the base class — worked around the immediate symptom, but the root cause (synchronous NUnit teardown fighting async Playwright transport) remained.

Separately, the service layer tests in `TimeTracker.Tests` already use xUnit — the industry-standard for .NET (used by Microsoft for ASP.NET Core, EF Core, and their own libraries). Having NUnit only in the Playwright project created a two-framework inconsistency.

**Decision:** Migrate `TimeTracker.Playwright` from NUnit to xUnit (`Microsoft.Playwright.Xunit`). Add a new `TimeTracker.ComponentTests` project using xUnit + bUnit for Blazor component-layer tests.

**Changes:**
- `[SetUpFixture]` / `[OneTimeSetUp]` / `[OneTimeTearDown]` → xUnit `ICollectionFixture<AppFixture>` with `[CollectionDefinition("App")]`
- `[SetUp]` / `[TearDown]` → `override InitializeAsync()` / `override DisposeAsync()` via xUnit's `IAsyncLifetime`
- `[Test]` → `[Fact]`; `[TestFixture]` removed; `[Explicit]` → `[Fact(Skip = "...")]`
- `Assert.Ignore()` for conditional skips → `[SkippableFact]` + `Skip.If()` (`Xunit.SkippableFact` package)
- `NUnit.RunSettings` `<NUnit>` section → `xunit.runner.json` (`stopOnFail`, `parallelizeTestCollections: false`)
- New `TimeTracker.ComponentTests` project: xUnit + bUnit; `MudBlazorContext` base class wires MudBlazor services + `MudPopoverProvider`; initial tests for `EntryRow` (10 tests) and `ProjectCard` (11 tests)

**Consequences:**
- ✅ Consistent xUnit across all three test layers
- ✅ `IAsyncLifetime` lifecycle has no synchronous teardown — eliminates the NUnit/Playwright async mismatch
- ✅ `ICollectionFixture` is the idiomatic xUnit pattern for shared one-time setup (app start + auth)
- ✅ bUnit component tests cover UI state and rendering logic without a browser — fast, deterministic
- ❌ bUnit cannot test full user journeys or network behaviour — Playwright E2E remains necessary for those

**Note on D010/D016:** Both decisions reference NUnit-specific implementation details (`[SetUpFixture]`, `[OneTimeSetUp]`, `Assert.Ignore()`). Those patterns are now superseded by the xUnit equivalents described above. The architectural decisions in D010/D016 (CI auth strategy, smoke test only in CI) are unchanged.

---

## D027: Showcase CSS unified via MSBuild

**Date:** 2026-06 — **Status:** Accepted — **Issue:** [#159](https://github.com/zkarachiwala/TimeTracker/issues/159)

**Context:** The showcase app (GitHub Pages) maintained a separate `wwwroot-showcase/css/showcase-app.css` copied manually from `TimeTracker.Web/wwwroot/css/app.css`. Between June 9 and June 21, `app.css` gained 51 lines of CSS (nav rail mini/closed state, mobile flash fix, calendar styles) that were never copied to `showcase-app.css`. The result: nav rail icon centering and calendar layout were broken in the showcase.

The structural problem: two files with no automated link. Every CSS change required a manual copy that could be — and was — forgotten.

**Decision:** Delete `showcase-app.css`. Have MSBuild copy `TimeTracker.Web/wwwroot/css/app.css` into the showcase publish output as `wwwroot/css/app.css` using a conditional `<Content>` ItemGroup gated on `-p:Showcase=true`. Update `wwwroot-showcase/index.html` to reference `css/app.css`. Add `ShowcaseFixture` + `ShowcaseTests` to `TimeTracker.Playwright` to catch regressions in showcase routing and rendering.

**Consequences:**
- ✅ Single CSS file to maintain — `app.css` changes automatically apply to the showcase with no extra steps
- ✅ MSBuild cross-project file reference is standard and supported; no custom build targets needed
- ✅ Showcase smoke tests (Part 2 of `docs/testing-strategy.md`) catch broken routing and rendering regressions
- ❌ Showcase always gets all CSS even if a feature is hidden behind `#if SHOWCASE`; acceptable trade-off (unused CSS has zero visible effect)
