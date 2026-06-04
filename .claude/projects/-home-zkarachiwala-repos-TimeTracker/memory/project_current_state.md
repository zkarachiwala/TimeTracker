---
name: project-current-state
description: "Current development state, completed phases, and next steps for TimeTracker"
metadata:
  type: project
---

Phase 8 (Azure deployment) complete — app deployed to App Service F1 + Azure SQL, GitHub Actions OIDC CI/CD live at timetracker-zak.azurewebsites.net.

**Architecture decisions locked (2026-06-04):**
- Stay on App Service F1 — hard free tier, no overage possible. This is a personal timesheeting tool, not a showcase; cost safety is a fundamental constraint.
- Custom domain via Cloudflare proxy — `timetracker.dzk.com.au` → `*.azurewebsites.net`. Cloudflare terminates TLS, provides free managed SSL. No ACA needed for this.
- ACA migration deferred as optional — `Dockerfile` kept in repo as migration artefact. If F1 is removed or limits bite, migration is a workflow change only (no app code is App Service-specific).
- WASM islands (Phase 11) confirmed feasible on App Service F1 — no containerisation required.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` required in App Service settings so HTTPS redirect and SameSite cookies work correctly behind Cloudflare proxy.

**Why ACA was rejected as primary target:**
- ACA Consumption has no hard spending cap — unexpected charges possible
- App is a personal tool (one user), not a portfolio showcase; F1 limits are non-issues in practice

**Phase 9 scope (next):**
- Add `Dockerfile` to repo root (migration artefact, not used in deployment)
- Configure Cloudflare proxy for custom domain
- Set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` in App Service

**Planned phases:**
- Phase 9: Cloudflare custom domain + Dockerfile artefact
- Phase 10: Playwright UX regression testing
- Phase 11: WASM islands on App Service F1 (remove SignalR)
- Phase 12: GitHub Pages showcase (planning session needed)

**Optional (not scheduled):**
- ACA migration — Dockerfile already done in Phase 9; workflow change only when/if needed

**Open backlog:**
- #32 soft-delete UX
- #34 app bar avatar
- #36 Zoho invoice export
