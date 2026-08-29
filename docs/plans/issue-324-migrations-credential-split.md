# EF design-time credential split rollout — issue #324

Companion to `docs/plans/migrations-principal-rollout.md` and `docs/plans/rls-bypass-rollout.md`. Those
rollouts narrowed the *production* app identity and replaced a blanket RLS exemption; this one does the
local-development equivalent — giving `dotnet ef` its own credential so the locally-running app can
finally hold the same minimal grant set as production (`db_datareader`+`db_datawriter` only, mirroring
`timetracker-zak`) instead of running as `sa`. See `docs/decisions.md` ADR-035 for the full rationale.

Ordered so there is no point where either `dotnet ef` or the running app is broken.

## 1. Code changes (this PR)

- [x] `ConnectionStringBuilder.Build` refactored to take `IConfiguration`/`bool isDevelopment` instead of `WebApplicationBuilder`; `IsDevelopmentEnvironment` helper added.
- [x] `Program.cs` call sites updated.
- [x] `TimeTrackerDataContextFactory` / `IdentityDataContextFactory` added (`TimeTracker.Web/Data/`), reading `MigrationsDbUser`/`MigrationsDbPassword`.
- [x] `ConnectionStringBuilderTests` added.
- [x] Dev container: `.devcontainer/create-app-login.sh`, `devcontainer.json` `postCreateCommand`, `docker-compose.yml` `app`/`db` env, `.env.example` updated.
- [x] Docs: `CLAUDE.md`, `docs/rls-security-model.md`, `.github/workflows/deploy.yml` comment, ADR-035.

At this stage, in every environment, `MigrationsDbUser`/`MigrationsDbPassword` point at the **same**
login the app's `DbUser`/`DbPassword` already use (`sa` locally) — same credential, new secret names,
no functional change yet. This isolates "does the factory wiring work" from "does narrowing the app's
own credential work," so a problem in either step is easy to attribute.

## 2. Verify the factories work, still against `sa`

From `TimeTracker.Web/`:
```bash
dotnet user-secrets set "MigrationsDbUser" "sa"
dotnet user-secrets set "MigrationsDbPassword" "<your local sa password>"

dotnet ef migrations add SanityCheck --context TimeTrackerDataContext
dotnet ef migrations remove --context TimeTrackerDataContext
```
Confirms the design-time factory builds and connects correctly before the credential-narrowing step.

From the **repo root** (mirrors CI's actual invocation — `--project` does not change the process's
working directory, which is what tripped up the original config-loading approach):
```bash
dotnet ef migrations script --idempotent --context TimeTrackerDataContext --project TimeTracker.Web/TimeTracker.Web.csproj --output /tmp/test.sql
dotnet ef migrations script --idempotent --context IdentityDataContext --project TimeTracker.Web/TimeTracker.Web.csproj --output /tmp/test-identity.sql
```

- [ ] Both commands complete without error, locally.

## 3. Create the `timetracker_app` login (WSL2)

Run once against your local SQL Server (`sqlcmd`/SSMS) — see `CLAUDE.md`'s migration-commands section
for the exact idempotent SQL. Server-level `CREATE LOGIN` first; the database-level `CREATE USER` +
role grants can run in the same session since `TimeTrackerDb` already exists on an established local
instance (unlike a fresh dev-container volume, where the two steps must be split around migrations —
see `.devcontainer/create-app-login.sh`).

- [ ] `timetracker_app` login created, `db_datareader`+`db_datawriter` granted on `TimeTrackerDb`.

## 4. Repoint the app's own credential (WSL2)

```bash
dotnet user-secrets set "DbUser" "timetracker_app"
dotnet user-secrets set "DbPassword" "<the password you set above>"
```
Restart `dotnet run`; exercise basic CRUD (create a time entry, list projects) — confirms
`db_datareader`+`db_datawriter` is sufficient at runtime and RLS still isolates per-user data exactly
as it does for `timetracker-zak` in production.

- [ ] App boots and basic CRUD works as `timetracker_app`.

## 5. Confirm migrations still work once the app is narrowed

```bash
cd TimeTracker.Web
dotnet ef migrations add TrivialCheck --context TimeTrackerDataContext
dotnet ef migrations remove --context TimeTrackerDataContext
```
This is the actual regression the whole change prevents: `MigrationsDbUser` (`sa`) must keep working
for schema changes after `DbUser` no longer can.

- [ ] Confirmed.

## 6. Dev container (best-effort)

Rebuild the container and confirm `postCreateCommand` completes: login creation → both
`dotnet ef database update` calls → grant step. Given the known, unrelated SSMS/firewall connectivity
issue with this environment, full end-to-end verification here isn't a hard requirement for this
issue — the goal is confirming the bootstrap script itself runs cleanly.

- [ ] `postCreateCommand` completes without error on a fresh rebuild.

## 7. Done

- [ ] All of the above checked off, `dotnet test TimeTracker.Tests && dotnet test TimeTracker.ComponentTests` green, PR raised.
