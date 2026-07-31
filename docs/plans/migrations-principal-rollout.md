# Migrations principal rollout — issue #322

Companion to `docs/plans/rls-bypass-rollout.md`. That rollout replaced a blanket `db_owner` RLS
exemption with a named role; this one replaces the app's own runtime identity as the thing that
runs schema migrations. See `docs/decisions.md` D031 for the full rationale.

**Read this before merging `claude/pipeline-migrations-322`.** The new `migrate` job in
`deploy.yml` will fail every deploy until the Azure/GitHub setup below exists — `Program.cs`'s
`MigrateAsync()` calls are deliberately left in place as a safety net until this is verified, so a
failing `migrate` job does not itself take the app down, but it does block `deploy` (`deploy` now
`needs: [check, migrate]`).

---

## 1. Provision the Azure AD principal

These steps are self-contained — you do not need to have run `docs/azure-deployment.md` Step 0
first. Set the resource names here:

```bash
RG=timetracker-rg
SERVER=timetracker-sql

SUB=$(az account show --query id -o tsv)
TENANT=$(az account show --query tenantId -o tsv)
```

Same pattern as `docs/azure-deployment.md` Step 7 (the deploy SP), naming this one
`timetracker-github-migrations`:

```bash
az ad app create --display-name "timetracker-github-migrations"

APP_ID=$(az ad app list --display-name "timetracker-github-migrations" --query "[0].appId" -o tsv)
APP_OID=$(az ad app list --display-name "timetracker-github-migrations" --query "[0].id" -o tsv)

az ad sp create --id $APP_ID
SP_OID=$(az ad sp show --id $APP_ID --query id -o tsv)

az ad app federated-credential create \
  --id $APP_OID \
  --parameters "{
    \"name\": \"timetracker-main\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:zkarachiwala/TimeTracker:ref:refs/heads/main\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"
```

- [ ] App registration + service principal created
- [ ] Federated credential created

## 2. Create and assign a dedicated firewall RBAC role

Same *shape* of role the backup principal has (D021: `firewallRules/write` + `firewallRules/delete`
only, scoped to the SQL server — not the built-in `SQL Server Contributor`, which grants more), but
its own separate role definition — **do not reuse the backup principal's role**
(`TimeTracker Backup Firewall Manager`). Backup and migrations are deliberately separate
principals so a compromise of one doesn't touch the other; sharing a role assignment between them
would recouple that audit boundary even though the permissions happen to be identical.

`AssignableScopes` is capped at the resource group, not the subscription, so this role definition
can never later be (mis)assigned to some other SQL server or resource group — only the one
`role assignment create` below grants anything, and only against `$SERVER`.

```bash
az role definition create --role-definition "{
  \"Name\": \"TimeTracker Migrations Firewall Manager\",
  \"Description\": \"Adds and removes a single SQL Server firewall rule for the GitHub Actions migrate workflow\",
  \"Actions\": [
    \"Microsoft.Sql/servers/firewallRules/write\",
    \"Microsoft.Sql/servers/firewallRules/delete\"
  ],
  \"AssignableScopes\": [\"/subscriptions/$SUB/resourceGroups/$RG\"]
}"

az role assignment create \
  --role "TimeTracker Migrations Firewall Manager" \
  --assignee-object-id $SP_OID \
  --assignee-principal-type ServicePrincipal \
  --scope /subscriptions/$SUB/resourceGroups/$RG/providers/Microsoft.Sql/servers/$SERVER
```

- [ ] Role created (separate from the backup principal's role)
- [ ] Role assigned, scoped to `timetracker-sql` only
- [ ] While here: narrow the pre-existing `TimeTracker Backup Firewall Manager` role's
      `AssignableScopes` from `/subscriptions/$SUB` to `/subscriptions/$SUB/resourceGroups/$RG` —
      same fix, different role (`az role definition update`, see `docs/azure-deployment.md` Step B)

## 3. SQL grants

Connect to `timetracker-sql.database.windows.net` / `TimeTrackerDb` with your Azure AD admin
account:

```sql
CREATE USER [timetracker-github-migrations] FROM EXTERNAL PROVIDER;

ALTER ROLE db_datareader ADD MEMBER [timetracker-github-migrations];
ALTER ROLE db_datawriter ADD MEMBER [timetracker-github-migrations];
ALTER ROLE db_ddladmin   ADD MEMBER [timetracker-github-migrations];
GRANT ALTER ANY SECURITY POLICY TO [timetracker-github-migrations];
ALTER ROLE rls_bypass    ADD MEMBER [timetracker-github-migrations];
```

`rls_bypass` matters even though this rollout doesn't add a data migration: per
`docs/rls-security-model.md`, a `FILTER` predicate applies to `UPDATE` as well as `SELECT`, so any
future backfill run without the bypass would touch zero rows and report success.

- [ ] User created, all four grants applied
- [ ] **Verify whether `db_ddladmin` already covers `CREATE FUNCTION`** — if the idempotent script
      in step 5 fails with a `CREATE FUNCTION` permission error despite `db_ddladmin`, add
      `GRANT CREATE FUNCTION TO [timetracker-github-migrations];` explicitly

## 4. GitHub secrets and variables

**Settings → Secrets and variables → Actions** — note the **Variables** tab specifically, not
Secrets. `SQL_RESOURCE_GROUP`/`SQL_SERVER`/`SQL_DATABASE` are plain resource identifiers, not
sensitive — they belong as Variables, same as the rest of this table:

| Type | Name | Value |
|------|------|-------|
| Secret | `MIGRATIONS_AZURE_CLIENT_ID` | `$APP_ID` from step 1 |
| Variable | `SQL_RESOURCE_GROUP` | `timetracker-rg` |
| Variable | `SQL_SERVER` | `timetracker-sql` |
| Variable | `SQL_DATABASE` | `TimeTrackerDb` |

These replace `BACKUP_RESOURCE_GROUP`/`BACKUP_SQL_SERVER`/`BACKUP_SQL_DATABASE` — those names were
backup-specific despite holding generic infrastructure identifiers that `backup.yml` and this
`migrate` job both need. `backup.yml` has been updated to read `SQL_RESOURCE_GROUP`/`SQL_SERVER`/
`SQL_DATABASE` too, so once these three exist, delete the three old `BACKUP_*` ones — nothing
should still reference them.

`AZURE_TENANT_ID`/`AZURE_SUBSCRIPTION_ID` are already shared secrets — no new entry needed.

- [ ] Secret and variables added (as Variables, not Secrets — this bit us once already)
- [ ] Old `BACKUP_RESOURCE_GROUP`/`BACKUP_SQL_SERVER`/`BACKUP_SQL_DATABASE` variables deleted once
      `backup.yml` is confirmed working against the new names (next nightly run, or trigger
      manually first)

## 5. Verify before relying on the real trigger

You do **not** need to merge this branch to main to test it, but the OIDC federated credential
from step 1 is scoped to `subject: repo:zkarachiwala/TimeTracker:ref:refs/heads/main` only — a
token issued for this branch presents a different subject claim
(`ref:refs/heads/claude/pipeline-migrations-322`) and Azure will reject it with
`AADSTS700213: No matching federated identity record found`. Add a second, temporary federated
credential scoped to this branch to test with, and remove it once verified — keep the permanent
one main-only:

```bash
MIGRATE_APP_OID=$(az ad app list --display-name "timetracker-github-migrations" --query "[0].id" -o tsv)

az ad app federated-credential create \
  --id $MIGRATE_APP_OID \
  --parameters "{
    \"name\": \"timetracker-pipeline-migrations-322-test\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:zkarachiwala/TimeTracker:ref:refs/heads/claude/pipeline-migrations-322\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"
```

- [ ] Temporary federated credential created

```
gh workflow run deploy.yml --ref claude/pipeline-migrations-322
```

- [ ] `migrate` job: firewall opens, artifact uploads (`migration-scripts-<sha>`), `sqlcmd` step
      exits 0, firewall closes (`az sql server firewall-rule list` shows no leaked
      `github-actions-migrate` rule after the run)
- [ ] If `deploy` itself doesn't run or is blocked: check **Settings → Environments →
      production → Deployment branches** — if it's restricted to `main` only, `migrate` (which has
      no `environment:` block) can still be verified from this branch even though `deploy` can't.
      That's fine for this step; `deploy` only needs to succeed once this merges to `main`.

Once `migrate` has a proven successful run, remove the temporary credential — the permanent one
(scoped to `main`) is what should be left in place after this branch merges:

```bash
CRED_ID=$(az ad app federated-credential list --id $MIGRATE_APP_OID \
  --query "[?name=='timetracker-pipeline-migrations-322-test'].id" -o tsv)

az ad app federated-credential delete --id $MIGRATE_APP_OID --federated-credential-id $CRED_ID
```

- [ ] Temporary federated credential removed
- [ ] Since `main` currently has no pending migrations, this run is a safe no-op — the idempotent
      script should apply nothing and still exit 0
- [ ] **Unverified from static review**: `mssql-tools18`'s bundled sqlcmd turned out to be the
      legacy ODBC-based tool with no access-token auth mode at all (confirmed via a real failed
      run — `Sqlcmd: '-authentication-method=...': Unknown Option`). Switched to installing
      `Microsoft.SqlServer.Sqlcmd` as a dotnet global tool (the modern go-sqlcmd) and passing the
      token via `SQLCMDPASSWORD` instead of a flag. This is still unverified against a real run —
      if it fails, check `~/.dotnet/tools/sqlcmd --help` in the job log for the actual syntax.
- [ ] `deploy` job still runs and succeeds afterward (it now depends on `migrate`)

## 6. Only after step 5 passes — revoke the old grants

```sql
REVOKE CREATE FUNCTION FROM [timetracker-zak];
REVOKE ALTER ANY SECURITY POLICY FROM [timetracker-zak];
```

- [ ] Grants revoked
- [ ] Redeploy (or `workflow_dispatch`) and confirm the app boots cleanly — `MigrateAsync()` is
      still in `Program.cs` at this point, but with nothing pending it doesn't need the revoked
      grants

## 7. Only after step 6 is confirmed stable — remove startup migration

This is a separate follow-up code change (Phase 3 of the issue #322 plan), not part of this
branch: remove `Database.MigrateAsync()` from `Program.cs` and replace it with a
`GetPendingMigrationsAsync()` guard that refuses to boot on a stale schema. Do not land this until
steps 1–6 above have a proven successful real deploy.

---

## Final state to confirm

| Principal | `db_datareader` | `db_datawriter` | `db_ddladmin` | `CREATE FUNCTION` | `ALTER ANY SECURITY POLICY` | `rls_bypass` |
|---|---|---|---|---|---|---|
| `timetracker-zak` (app) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `timetracker-github-migrations` (CI, new) | ✅ | ✅ | ✅ | ✅ (via `db_ddladmin` or explicit) | ✅ | ✅ |
| `timetracker-github-backup` (CI, existing) | ✅ | ❌ | ✅ | — | — | ✅ |
