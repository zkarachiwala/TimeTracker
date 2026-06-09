# Technical debt register

Every entry records a decision that was forced by a constraint (cost, tier limitation, or missing infrastructure) rather than best practice. The register exists so future-you — or a future team — knows exactly what would need addressing to make this production-grade.

**Numbering is permanent.** Never reuse or renumber an ID. When an item is resolved, update its Status cell and leave it in place. New items are always appended with the next sequential number. If the active list grows unwieldy, move resolved rows to the [Resolved](#resolved) section at the bottom.

---

## Active

### Infrastructure & compute

| # | Status | Decision | Constraint | Consequence | Proper solution |
|---|--------|----------|-----------|-------------|-----------------|
| TD1 | | Global WASM rendering — server CPU offloaded to browser | Azure F1: 60 CPU-min/day. SSR re-renders on every navigation; WASM serves static files + API only. Explicitly chosen to stay within F1 budget. | Cold start: browser downloads ~5 MB WASM bundle before first render. No server-side rendering for SEO (acceptable — app requires auth). MudBlazor SSR incompatibility also contributed. | Upgrade to Standard tier. Revisit SSR+WASM islands or keep global WASM (it's also a valid architectural preference, not just a cost workaround). See [D001](decisions.md#d001-global-wasm-rendering-over-ssr--wasm-islands). |
| TD2 | | Single instance, no autoscaling | F1 tier: no deployment slots, no scale sets, no multi-instance. | One bad deploy or one process crash = full outage. No horizontal scale-out. Rollback requires manual redeploy of previous commit. | Standard tier: deployment slots (blue/green), autoscaling rules, health check probes. |
| TD3 | | No blue/green deployment or smoke test gate | F1: no deployment slots. | Every deploy goes straight to production. No pre-promotion testing window. | Staging slot (Standard tier): deploy → staging, run smoke tests, swap slots. |
| TD4 | | Azure SQL free offer (serverless, auto-pause) | Zero-cost. SQL serverless auto-pauses after 1h idle; cold start adds ~5–10s latency on first query after pause. 7-day backup retention only. | First request after idle period is slow. If data were lost, recovery window is 7 days. No long-term retention. | Azure SQL Standard/Business Critical: guaranteed IOPS, always-on compute, 35-day backup retention, geo-replication. |
| TD5 | | No connection pool tuning | Free tier: low traffic, single instance, no need for pooling optimisation. | Default EF Core pooling in use. Under load, connection exhaustion is possible. | Explicit `Min Pool Size` / `Max Pool Size` in connection strings tuned to expected concurrency; connection resiliency with retry policy for transient failures. |

### CI/CD & testing

| # | Status | Decision | Constraint | Consequence | Proper solution |
|---|--------|----------|-----------|-------------|-----------------|
| TD6 | | Authenticated E2E tests excluded from CI | No staging environment. Authenticated tests against production would require a privileged auth bypass endpoint — assessed as unacceptable for an app handling real billing data. | CI validates only 9 unauthenticated tests. Regressions in authenticated flows are caught by local Playwright runs only. Pre-push hook partially mitigates. | Staging environment (see TD3) with dedicated Google OAuth app registration. Authenticated tests run against staging in CI, gate production promote. See [D010](decisions.md#d010-playwright-ci-auth--unauthenticated-tests-only-in-ci). |
| TD7 | | E2E tests run against production after every deploy | No staging (see TD6). | Playwright targets the live app. Write tests cannot be enabled in CI without risking real data mutation. | Staging environment. CI runs full suite against staging; production never touched by automated tests. |
| TD8 | | No SAST, dependency scanning, or secret scanning in CI | Not configured. | Vulnerable dependencies and accidental secret commits are not detected automatically. | Add `dotnet list package --vulnerable` to CI; add GitHub secret scanning (free for public repos); add OWASP dependency-check or Dependabot. |
| TD22 | | Pre-push Playwright hook relies on a manually-maintained local dev server | No staging environment (TD3). The hook is the only pre-production E2E gate for unauthenticated flows; it must run against something. The local dev server is the only option. | Asset fingerprints change on every source edit. The running dev server becomes stale; pre-push tests fail with cryptic MIME-type or PDB errors unless the developer restarts `dotnet run` before pushing. Mitigated partially by staleness detection in the hook, but a restart is still required on every non-trivial source change. | Staging environment (see TD3, TD6). CI runs Playwright against staging before production promote; local hook can be removed or made advisory. See [D015](decisions.md#d015-showcase-static-assets-isolated-to-wwwroot-showcase). |

### Authentication & authorisation

| # | Status | Decision | Constraint | Consequence | Proper solution |
|---|--------|----------|-----------|-------------|-----------------|
| TD9 | | Access control via `AllowedEmails` config list, not RBAC | Personal app with one user. A database-backed role/permission system adds complexity with no benefit at this scale. | Adding a new user requires a config change and redeploy. No role granularity. | `UserRoles` / `Permissions` table. Admin UI for user management. Dynamic role assignment without deploys. |
| TD10 | | Google OAuth only — no email/password fallback, no MFA | Minimal auth stack. Google is free; no paid IdP needed. | If Google OAuth is unavailable, login is impossible. No way to authenticate without a Google account. No MFA. | Second auth provider (email/password with ASP.NET Identity) as fallback. TOTP or WebAuthn MFA. Consider Entra ID or Auth0 for enterprise SSO. |
| TD11 | | No session revocation / "logout everywhere" | Single-user; 1-day cookie expiry deemed sufficient. | A stolen cookie cannot be invalidated server-side until it expires. No audit log of auth events. | Server-side session store (Redis or DB); revocation endpoint; auth event audit log. |
| TD12 | | Google OAuth app registration tied to production domain only | One registration for simplicity. Separate staging would require a second OAuth app in Google Console. | Staging environment (if added) cannot use the same OAuth app. New registration needed per environment. | One OAuth app per environment: `timetracker-staging.azurewebsites.net` uses its own client ID and redirect URIs. |

### Observability & operations

| # | Status | Decision | Constraint | Consequence | Proper solution |
|---|--------|----------|-----------|-------------|-----------------|
| TD13 | | No APM or structured logging — ASP.NET default console log only | Application Insights adds cost and dependency. Serilog adds complexity not justified for a personal app. | No request tracing, no correlation IDs, no performance baselines, no alerting on error spikes or latency. Silent failures. | Application Insights (or OpenTelemetry → Datadog/Grafana). Serilog with structured JSON output. Uptime monitor (UptimeRobot free tier covers this at zero cost). |
| TD14 | | No global exception handler — unhandled exceptions return default ASP.NET error page | Personal app, error surface is low. | Unhandled exceptions expose stack traces in development and return unhelpful responses in production. No RFC 7807 Problem Details format. | `app.UseExceptionHandler` + `IProblemDetailsService`. Structured error codes. Exception telemetry to APM. |
| TD15 | | No backup / DR runbook | Data loss on a personal app is an inconvenience, not a business event. | No documented RTO/RPO. No tested restore procedure. If Azure SQL free offer is deprecated or the subscription lapses, data recovery is uncertain. | Document RTO/RPO. Quarterly restore drill. Cross-region backup. Export automation (nightly JSON export to blob storage). |

### Security

| # | Status | Decision | Constraint | Consequence | Proper solution |
|---|--------|----------|-----------|-------------|-----------------|
| TD16 | | Secrets in Azure App Service settings, not Key Vault | Key Vault adds cost and setup. App Service settings are encrypted at rest — sufficient for personal app. | Any user with Contributor RBAC on the resource group can read all secrets in the portal. No secret rotation. No audit log of secret access. | Azure Key Vault with Managed Identity authentication. Automatic rotation for DB credentials. Access policy scoped to app identity only. |
| TD17 | | `unsafe-inline` in CSP `style-src` | MudBlazor injects inline styles at runtime. Removing `unsafe-inline` would require migrating to a CSP-compatible component library (estimated 5–10 person-days). | Inline style injection attacks (e.g. CSS exfiltration via inline `style` attributes) are not blocked by CSP. | Migrate to Fluent UI Blazor or another CSP-compatible library. Remove `unsafe-inline`. Add `style-src-elem` nonce-based policy. See [D002](decisions.md#d002-mudblazor-over-fluent-ui-blazor-and-bootstrap) for migration cost estimate. |
| TD18 | | Rate limiting on auth endpoints only — no per-user or per-endpoint limits on CRUD APIs | Personal app: single user, low traffic, abuse risk is negligible. | A compromised session can make unlimited API calls. No per-user throttle to detect anomalous activity. | Rate limiting on all mutating endpoints. Per-user limits (not just global). Distributed rate limiting (Redis) when scaling beyond one instance. |
| TD19 | | Data isolation enforced at service layer only — no database-level row security | Personal app; only one real user. DB-level RLS adds schema and migration complexity with no practical benefit at current scale. | If `IUserContextService` is bypassed (e.g. a future API endpoint omitting the user filter), cross-user data access is possible. No defence in depth. | SQL Server Row-Level Security policies. Cross-tenant access integration tests. Audit trail (CreatedBy, UpdatedBy, DeletedBy) on all entities. |
| TD20 | | `AllowedHosts: "*"` in `appsettings.json` | Default; no explicit origin restriction configured. | Accepts HTTP Host headers from any origin — relevant only if the app were load-balanced behind a proxy that doesn't set the Host header correctly. Low practical risk with Cloudflare proxy. | Restrict `AllowedHosts` to `timetracker.dzk.com.au;timetracker-zak.azurewebsites.net`. |

### CDN & networking

| # | Status | Decision | Constraint | Consequence | Proper solution |
|---|--------|----------|-----------|-------------|-----------------|
| TD21 | | Cloudflare free plan for CDN and DDoS protection | Zero-cost. Free plan provides basic DDoS mitigation, no WAF custom rules, limited analytics. | WAF is limited to Cloudflare-managed rules only (no custom rules, no rate-limit rules at CDN layer). No bot management. No advanced analytics. | Cloudflare Pro/Business (WAF custom rules, rate limiting, analytics) or Azure Front Door Premium (native Azure integration, managed WAF, analytics). |

---

## Resolved

*No items resolved yet. When an item is closed, move its row here and update the Status cell: `✅ Resolved YYYY-MM — brief note on what was done`.*

---

*ADRs for decisions referenced above live in [docs/decisions.md](decisions.md). Tech debt entries link directly to the relevant decision.*
