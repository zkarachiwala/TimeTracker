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

## 2. Assign the firewall RBAC role

Same custom role the backup principal has (D021: `firewallRules/write` + `firewallRules/delete`
only, scoped to the SQL server — not the built-in `SQL Server Contributor`, which grants more).
Reuse the existing custom role definition if you still have its name/ID from the backup rollout;
otherwise look it up:

```bash
ROLE_ID=$(az role definition list --custom-role-only true --query "[?roleName=='<custom-role-name>'].name" -o tsv)

az role assignment create \
  --role $ROLE_ID \
  --subscription $SUB \
  --assignee-object-id $SP_OID \
  --assignee-principal-type ServicePrincipal \
  --scope /subscriptions/$SUB/resourceGroups/<RG>/providers/Microsoft.Sql/servers/timetracker-sql
```

- [ ] Role assigned, scoped to `timetracker-sql` only

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

**Settings → Secrets and variables → Actions:**

| Type | Name | Value |
|------|------|-------|
| Secret | `MIGRATIONS_AZURE_CLIENT_ID` | `$APP_ID` from step 1 |
| Variable | `SQL_RESOURCE_GROUP` | Same resource group as `BACKUP_RESOURCE_GROUP` |
| Variable | `SQL_SERVER` | `timetracker-sql` (same as `BACKUP_SQL_SERVER`) |
| Variable | `SQL_DATABASE` | `TimeTrackerDb` (same as `BACKUP_SQL_DATABASE`) |

`AZURE_TENANT_ID`/`AZURE_SUBSCRIPTION_ID` are already shared secrets — no new entry needed.

- [ ] Secret and variables added

## 5. Verify before relying on the real trigger

```
gh workflow run deploy.yml
```

- [ ] `migrate` job: firewall opens, artifact uploads (`migration-scripts-<sha>`), `sqlcmd` step
      exits 0, firewall closes (`az sql server firewall-rule list` shows no leaked
      `github-actions-migrate` rule after the run)
- [ ] Since `main` currently has no pending migrations, this run is a safe no-op — the idempotent
      script should apply nothing and still exit 0
- [ ] **Unverified from static review**: the exact `sqlcmd` flag syntax
      (`--authentication-method=ActiveDirectoryAccessToken --access-token ...`) for the
      `mssql-tools18` version installed by the workflow's apt step. If this step fails, check
      `sqlcmd --version` in the job log and adjust the flags to match.
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
