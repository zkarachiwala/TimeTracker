# Azure Deployment Setup

One-time steps to provision and wire up the Azure resources for TimeTracker.
After this guide is complete, every push to `main` that passes CI deploys automatically.

---

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- An Azure subscription (free tier is sufficient)
- Google OAuth credentials (see `docs/google-oauth-setup.md`)

---

## Step 0 — Log in and set your variables

Run these once in your terminal. Every command below uses these variables — nothing else needs editing.

```bash
az login

# Choose a globally unique name for the App Service
# It becomes the URL: https://<APP>.azurewebsites.net
APP=timetracker-zak

# Your Google OAuth credentials (from Google Cloud Console)
GOOGLE_CLIENT_ID=<paste-here>
GOOGLE_CLIENT_SECRET=<paste-here>

# Email address allowed to log in
ALLOWED_EMAIL=zak@dzk.com.au

# Fixed names — change only if you want something different
RG=timetracker-rg
SERVER=timetracker-sql
DB=TimeTrackerDb
PLAN=timetracker-plan
LOCATION=australiaeast

# Derived — do not edit
ADMIN_EMAIL=$(az ad signed-in-user show --query mail -o tsv)
ADMIN_OID=$(az ad signed-in-user show --query id -o tsv)
CONN="Server=${SERVER}.database.windows.net;Database=${DB};Authentication=Active Directory Managed Identity;Encrypt=True"
```

---

## Step 1 — Resource group

```bash
az group create --name $RG --location $LOCATION
```

---

## Step 2 — Azure SQL Database (free offer)

```bash
az sql server create \
  --resource-group $RG \
  --name $SERVER \
  --location $LOCATION \
  --enable-ad-only-auth \
  --external-admin-principal-type User \
  --external-admin-name $ADMIN_EMAIL \
  --external-admin-sid $ADMIN_OID

az sql db create \
  --resource-group $RG \
  --server $SERVER \
  --name $DB \
  --edition GeneralPurpose \
  --compute-model Serverless \
  --family Gen5 \
  --capacity 2 \
  --use-free-limit \
  --free-limit-exhaustion-behavior AutoPause

az sql server firewall-rule create \
  --resource-group $RG \
  --server $SERVER \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

`--enable-ad-only-auth` disables SQL username/password login entirely. Only Azure AD / Managed Identity accounts can connect.

---

## Step 3 — App Service (F1 free plan)

```bash
az appservice plan create \
  --resource-group $RG \
  --name $PLAN \
  --sku F1 \
  --is-linux

az webapp create \
  --resource-group $RG \
  --plan $PLAN \
  --name $APP \
  --runtime "DOTNETCORE:10.0"
```

---

## Step 4 — Enable Managed Identity

```bash
az webapp identity assign \
  --resource-group $RG \
  --name $APP

# Capture the identity's object ID for the next step
MI_OID=$(az webapp identity show \
  --resource-group $RG \
  --name $APP \
  --query principalId -o tsv)
```

---

## Step 5 — Grant the Managed Identity access to SQL

This must be run by your Azure AD admin account (which you logged in as in Step 0).

```bash
az sql db show-connection-string \
  --server $SERVER \
  --name $DB \
  --client ado.net
```

Then open **Azure Data Studio** (or any SQL client that supports Azure AD login), connect to `${SERVER}.database.windows.net` with your Azure AD account, and run:

```sql
CREATE USER [<APP>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<APP>];
ALTER ROLE db_datawriter ADD MEMBER [<APP>];
```

Replace `<APP>` with the value you set for `APP` above (e.g. `timetracker-zak`).

### Step 5b — Grant Row-Level Security permissions

The EF Core migration that installs RLS (`AddAuditTrailAndRowLevelSecurity`) creates SQL functions and security policies. These DDL operations require two additional grants beyond `db_datareader`/`db_datawriter`. Run as admin in the same SQL session:

```sql
GRANT CREATE FUNCTION TO [<APP>];
GRANT ALTER ANY SECURITY POLICY TO [<APP>];
```

> **Why these are separate:** `db_datareader` and `db_datawriter` grant DML access only. Schema-level DDL (`CREATE FUNCTION`) and security infrastructure (`ALTER ANY SECURITY POLICY`) require explicit grants. These are the minimum permissions needed; no elevation to `db_owner` or `db_ddladmin` is required.
>
> **db_owner exemption:** `db_owner` and `sysadmin` users are exempt from RLS by SQL Server design. The production Managed Identity holds only `db_datareader + db_datawriter`, so RLS **is** enforced in production. Local dev uses `sa` (sysadmin), so RLS is bypassed locally — this is intentional.

---

## Step 6 — Configure App Service

```bash
az webapp config connection-string set \
  --resource-group $RG \
  --name $APP \
  --connection-string-type SQLAzure \
  --settings \
    TimeTrackerConnection="$CONN" \
    IdentityConnection="$CONN"

az webapp config appsettings set \
  --resource-group $RG \
  --name $APP \
  --settings \
    "Authentication__Google__ClientId=${GOOGLE_CLIENT_ID}" \
    "Authentication__Google__ClientSecret=${GOOGLE_CLIENT_SECRET}" \
    "Authentication__AllowedEmails__0=${ALLOWED_EMAIL}" \
    "ASPNETCORE_ENVIRONMENT=Production"
```

---

## Step 7 — Set up OIDC authentication for GitHub Actions

The deploy workflow authenticates to Azure using OpenID Connect (OIDC) — no stored credentials anywhere.

### 7a — Create the app registration and service principal

```bash
# Create the app registration
az ad app create --display-name "timetracker-github-deploy"

# Capture both the client ID (appId) and the object ID (id) — you need both
APP_ID=$(az ad app list --display-name "timetracker-github-deploy" --query "[0].appId" -o tsv)
APP_OID=$(az ad app list --display-name "timetracker-github-deploy" --query "[0].id" -o tsv)

# Create the service principal
az ad sp create --id $APP_ID
SP_OID=$(az ad sp show --id $APP_ID --query id -o tsv)

# Capture tenant and subscription IDs
TENANT=$(az account show --query tenantId -o tsv)
SUB=$(az account show --query id -o tsv)
```

### 7b — Assign the minimum required role

`Website Contributor` scoped to just your App Service — no broader access needed:

```bash
az role assignment create \
  --role "Website Contributor" \
  --subscription $SUB \
  --assignee-object-id $SP_OID \
  --assignee-principal-type ServicePrincipal \
  --scope /subscriptions/$SUB/resourceGroups/$RG/providers/Microsoft.Web/sites/$APP
```

### 7c — Create the federated credential

This tells Azure to trust tokens issued by GitHub Actions for the `main` branch of your repo:

```bash
az ad app federated-credential create \
  --id $APP_OID \
  --parameters "{
    \"name\": \"timetracker-main\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:zkarachiwala/TimeTracker:ref:refs/heads/main\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"
```

### 7d — Print the values you need for GitHub

```bash
echo "AZURE_CLIENT_ID:       $APP_ID"
echo "AZURE_TENANT_ID:       $TENANT"
echo "AZURE_SUBSCRIPTION_ID: $SUB"
```

### 7e — Add to GitHub

In your repository go to **Settings → Secrets and variables → Actions** and add:

| Type | Name | Value |
|------|------|-------|
| Secret | `AZURE_CLIENT_ID` | From step 7d |
| Secret | `AZURE_TENANT_ID` | From step 7d |
| Secret | `AZURE_SUBSCRIPTION_ID` | From step 7d |
| Variable | `AZURE_WEBAPP_NAME` | The value of `APP` (e.g. `timetracker-zak`) |

---

## Step 8 — First deployment

Push any commit to `main` (or re-run the Deploy workflow from the Actions tab). Once CI passes, the Deploy workflow authenticates via OIDC, publishes, and deploys. Migrations run automatically on startup.

Check progress at: `https://github.com/zkarachiwala/TimeTracker/actions`

Your app will be live at: `https://${APP}.azurewebsites.net`

---

## Step 9 — Custom domain

**Not applicable on the F1 free tier.** Azure App Service F1 does not support custom domains. The app is served at `https://${APP}.azurewebsites.net` — Azure provides TLS via the default `*.azurewebsites.net` wildcard certificate at no cost. No further configuration is needed.

Upgrading to at least the Basic (B1) tier unlocks custom domain binding and App Service Managed Certificates. See [D017](decisions.md#d017-cloudflare-free-plan-over-paid-cdnwaf) for the rationale.

---


## Database Backup Setup

A GitHub Actions workflow (`.github/workflows/backup.yml`) exports a `.bacpac` nightly at 02:00 UTC and pushes it to a private GitHub repository (`TimeTracker-backups`). It uses a dedicated service principal with the minimum possible permissions — separate from the deploy SP.

### How it works

#### What is a `.bacpac`?

A `.bacpac` is a self-contained export format produced by Microsoft's SqlPackage tool. It contains the full database schema and all data in a single file. You can import it into any SQL Server instance to restore the database to the state it was in at export time.

#### Why a private GitHub repository?

The obvious alternative — GitHub Actions artifacts — is not safe for a public repository. Artifacts on public repos are downloadable by anyone without authentication. A `.bacpac` contains all user data, so it must be stored somewhere private. A separate private repository (`TimeTracker-backups`) is used instead. The backup workflow pushes files there using a fine-grained PAT scoped exclusively to that one repo (contents: write, nothing else).

#### Why can't the workflow use a SQL username and password?

The SQL server is created with `--enable-ad-only-auth`, which disables SQL username/password login entirely. Only Azure AD identities can connect. The workflow authenticates as an Azure AD service principal (`timetracker-github-backup`) and exchanges that identity for a short-lived database access token — no password ever exists.

#### The flow, step by step

Each nightly run at 02:00 UTC does the following:

1. **Azure login via OIDC** — GitHub issues a short-lived token to the Actions runner; Azure trusts it because a federated credential links this repo's `main` branch to the `timetracker-github-backup` service principal. No client secrets are stored anywhere.
2. **Firewall punch-through** — GitHub Actions runners get a different public IP every run. The workflow fetches the runner's current IP and opens a SQL Server firewall rule for exactly that IP. The rule is removed at the end of the run regardless of success or failure (`if: always()`).
3. **SqlPackage export** — the service principal exchanges its Azure AD identity for a short-lived SQL access token and passes it to SqlPackage. The output is a `backup-YYYY-MM-DD.bacpac` file on the runner's disk.
4. **Push to private repo** — the `.bacpac` is cloned into `TimeTracker-backups` and committed. Any files whose filename date is older than 30 days are deleted first, so the repo always holds a rolling 30-day window of backups.
5. **Firewall rule removed** — the temporary rule opened in step 2 is deleted.

#### Why `db_owner` for the backup SQL user?

Two constraints make `db_owner` the minimum viable permission for export:

- **SqlPackage requires `DBCC SHOW_STATISTICS`** to analyse indexes during export. This permission requires `db_owner` or `db_ddladmin`; `db_datareader` alone is insufficient.
- **Row-Level Security** — the app schema enforces RLS policies that filter data by `SESSION_CONTEXT(N'UserId')`. A backup user has no session context, so a `db_datareader` account would export empty tables. `db_owner` is exempt from RLS by SQL Server design, ensuring the full dataset is captured.

The Azure RBAC side remains tightly constrained — the service principal can only write and delete firewall rules on this one SQL server. `db_owner` on the database does not expand that.

**Credential summary:**
- Separate app registration (`timetracker-github-backup`) from the deploy SP
- Custom Azure role: `firewallRules/write` + `firewallRules/delete` only, scoped to the SQL server resource
- SQL user: `db_owner` on `TimeTrackerDb`
- OIDC for Azure login (no client secrets); fine-grained PAT for the backup repo push (no broad GitHub token)

### Step A — Set variables and create the app registration

These steps are self-contained — you do not need to have run Step 0 first. Set the resource names here:

```bash
RG=timetracker-rg
SERVER=timetracker-sql

SUB=$(az account show --query id -o tsv)
TENANT=$(az account show --query tenantId -o tsv)

az ad app create --display-name "timetracker-github-backup"

BACKUP_APP_ID=$(az ad app list --display-name "timetracker-github-backup" --query "[0].appId" -o tsv)
BACKUP_APP_OID=$(az ad app list --display-name "timetracker-github-backup" --query "[0].id" -o tsv)

az ad sp create --id $BACKUP_APP_ID
BACKUP_SP_OID=$(az ad sp show --id $BACKUP_APP_ID --query id -o tsv)
```

### Step B — Create a minimal custom Azure role

```bash
az role definition create --role-definition "{
  \"Name\": \"TimeTracker Backup Firewall Manager\",
  \"Description\": \"Adds and removes a single SQL Server firewall rule for the GitHub Actions backup workflow\",
  \"Actions\": [
    \"Microsoft.Sql/servers/firewallRules/write\",
    \"Microsoft.Sql/servers/firewallRules/delete\"
  ],
  \"AssignableScopes\": [\"/subscriptions/$SUB\"]
}"
```

### Step C — Assign the role, scoped to the SQL server only

```bash
az role assignment create \
  --role "TimeTracker Backup Firewall Manager" \
  --assignee-object-id $BACKUP_SP_OID \
  --assignee-principal-type ServicePrincipal \
  --scope /subscriptions/$SUB/resourceGroups/$RG/providers/Microsoft.Sql/servers/$SERVER
```

This SP can add and remove firewall rules on this one SQL server. It cannot read or modify any other Azure resource.

### Step D — Create the federated credential

```bash
az ad app federated-credential create \
  --id $BACKUP_APP_OID \
  --parameters "{
    \"name\": \"timetracker-backup-main\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:zkarachiwala/TimeTracker:ref:refs/heads/main\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"
```

### Step E — Create the SQL database user

Connect to `timetracker-sql.database.windows.net` with your Azure AD admin account (Azure Data Studio or `sqlcmd`) and run against `TimeTrackerDb`:

```sql
CREATE USER [timetracker-github-backup] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [timetracker-github-backup];
```

`db_owner` is required for two reasons specific to this database:

1. **SqlPackage needs `DBCC SHOW_STATISTICS`** to analyse indexes during export — this requires `db_owner` or `db_ddladmin`; `db_datareader` alone is insufficient.
2. **RLS bypasses** — the app schema has Row-Level Security policies that filter data by `SESSION_CONTEXT(N'UserId')`. A backup user has no session context, so a `db_datareader` account would export empty tables. `db_owner` is exempt from RLS by SQL Server design, ensuring the full dataset is captured.

The Azure RBAC side of this SP is still tightly locked (firewall rule write/delete only, scoped to the SQL server resource) — `db_owner` on the database does not expand that.

### Step F — Create the private backup repository

Create a **private** repository at `github.com/zkarachiwala/TimeTracker-backups` (or any name you choose — must be private). Leave it empty; the workflow will push `.bacpac` files to it daily.

### Step G — Create a fine-grained PAT for the backup repo

Go to **GitHub → Settings → Developer settings → Personal access tokens → Fine-grained tokens → Generate new token**:

| Field | Value |
|-------|-------|
| Token name | `TimeTracker backup push` |
| Expiration | 1 year (maximum for fine-grained PATs — set a calendar reminder to rotate) |
| Resource owner | `zkarachiwala` |
| Repository access | Only select repositories → `TimeTracker-backups` |
| Repository permissions | Contents → **Read and write** (all others: No access) |

### Step H — Add secrets and variables to GitHub

```bash
echo "BACKUP_AZURE_CLIENT_ID: $BACKUP_APP_ID"
```

In **Settings → Secrets and variables → Actions** on the `TimeTracker` repo add:

| Type | Name | Value |
|------|------|-------|
| Secret | `BACKUP_AZURE_CLIENT_ID` | From above |
| Secret | `BACKUP_REPO_TOKEN` | Fine-grained PAT from Step G |
| Variable | `BACKUP_RESOURCE_GROUP` | `timetracker-rg` |
| Variable | `BACKUP_SQL_SERVER` | `timetracker-sql` |
| Variable | `BACKUP_SQL_DATABASE` | `TimeTrackerDb` |
| Variable | `BACKUP_REPO_NAME` | `TimeTracker-backups` (or whatever you named it) |

`AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` are reused from the deploy setup.

### Verifying

Run the workflow manually from **Actions → Database Backup → Run workflow**. A `.bacpac` file will be committed to the private `TimeTracker-backups` repo. Check that the firewall rule `github-actions-backup` is absent from the SQL server after the run completes (the cleanup step removes it unconditionally).

---

## Free tier limits

| Resource | Plan | Limit | When exceeded |
|----------|------|-------|---------------|
| App Service | F1 | 60 CPU min/day, 1 GB RAM, sleeps after 20 min idle | Throttled, no charge |
| Azure SQL | Free offer | 32 GB data, 32 GB backup | Auto-pauses, no charge |
| GitHub Actions | Free | 2,000 min/month, 500 MB artifact storage | Blocked until next cycle |
