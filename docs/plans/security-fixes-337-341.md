# Security bug batch: #337–#341

Working through five independently-filed security defects, one branch/PR per issue. No merges without explicit approval.

## Order and status

1. **#337** — Drop `app.ProjectsUserPolicy` (RLS contradicts ADR-024; project visibility belongs in app tier). Migration removing the policy/predicate. — **PR #375 merged**
2. **#338** — Enforce `ProjectUser` membership check in `TimeEntryService.CreateTimeEntry`/`UpdateTimeEntry` before assigning `ProjectId`. Reuse `ProjectService.GetAssignedProjects` predicate. Unit test: unassigned user rejected on create + update. — **PR #376 open**
3. **#341** — Three small endpoint auth fixes (grouped by the issue itself):
   - `/api/dev/login` needs a non-guessable token check in addition to environment gate — done, with `DevTools:LoginToken` config + fixed-time comparison; `AppFixture.cs` updated to send it automatically (preserves ADR-016's fully-automated auth flow)
   - `/api/auth/revoke-sessions` dropped from Admin-only to `RequireAuthorization()` (self-service op) — done
   - `/api/projects/{id}/users` moved from plain `group` to `adminGroup` — done
   — **PR #378 open**. Along the way, found and filed a separate pre-existing bug (#377, High priority): `ProjectDetailPage.razor` breaks for all non-admin users because `Reload()` atomically awaits an admin-only `/api/admin/users` call — not caused by this PR, not fixed by it either, deliberately left for its own PR.
4. **#339** — Rotate security stamp in `UserManagementService.SetAdminRoleAsync`; fix `CookieAuthenticationStateProvider` to drop cached auth state on 401 instead of caching indefinitely. Integration test: demoted admin rejected immediately, not after the 30-min validation interval. — pending
5. **#340** — `UserSessionContextInterceptor.SetSessionContextAsync`: set `SESSION_CONTEXT` to `NULL` explicitly for unauthenticated connections instead of returning early (fail-closed, not fail-open). Also fix stale XML doc comment about db_owner RLS exemption. Container test: pooled connection reused by unauthenticated context sees zero rows. Requires Docker. — pending

## Notes

- Each fix ships with its own test per the issue's "Verification" section.
- #340 needs the container test suite (Docker) — verify with `dotnet test TimeTracker.Tests --filter "Category=Container"`.
- Fast suite (`Category!=Container` + ComponentTests) run after every fix; full Playwright suite is the user's to run before merge, per project convention.
- This plan file was independently created on the #337, #338, and #341 branches (each branched from `main` before the others merged) — resolved via merge as each PR merged; no further conflicts expected once #376 also merges.
