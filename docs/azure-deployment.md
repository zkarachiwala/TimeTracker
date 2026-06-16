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

A GitHub Actions workflow (`.github/workflows/backup.yml`) exports a `.bacpac` nightly at 02:00 UTC and stores it as a 90-day artifact. It uses a dedicated service principal with the minimum possible permissions — separate from the deploy SP.

**Credential model:**
- Separate app registration (`timetracker-github-backup`) from the deploy SP
- Custom Azure role: only `firewallRules/write` + `firewallRules/delete`, scoped to the SQL server resource
- SQL user: `db_datareader` + `VIEW DATABASE STATE` + `VIEW DEFINITION` — no `db_owner`
- OIDC (no client secrets stored anywhere)

### Step A — Create the app registration and service principal

```bash
az ad app create --display-name "timetracker-github-backup"

BACKUP_APP_ID=$(az ad app list --display-name "timetracker-github-backup" --query "[0].appId" -o tsv)
BACKUP_APP_OID=$(az ad app list --display-name "timetracker-github-backup" --query "[0].id" -o tsv)

az ad sp create --id $BACKUP_APP_ID
BACKUP_SP_OID=$(az ad sp show --id $BACKUP_APP_ID --query id -o tsv)

TENANT=$(az account show --query tenantId -o tsv)
SUB=$(az account show --query id -o tsv)
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

Connect to `${SERVER}.database.windows.net` with your Azure AD admin account (Azure Data Studio or `sqlcmd`) and run against `TimeTrackerDb`:

```sql
CREATE USER [timetracker-github-backup] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [timetracker-github-backup];
GRANT VIEW DATABASE STATE TO [timetracker-github-backup];
GRANT VIEW DEFINITION TO [timetracker-github-backup];
```

`VIEW DATABASE STATE` is required by SqlPackage to read internal database state during export.
`VIEW DEFINITION` is required to read schema object definitions for the `.bacpac`.

### Step F — Add to GitHub

```bash
echo "BACKUP_AZURE_CLIENT_ID: $BACKUP_APP_ID"
```

In **Settings → Secrets and variables → Actions** add:

| Type | Name | Value |
|------|------|-------|
| Secret | `BACKUP_AZURE_CLIENT_ID` | From above |
| Variable | `BACKUP_RESOURCE_GROUP` | `timetracker-rg` |
| Variable | `BACKUP_SQL_SERVER` | `timetracker-sql` |
| Variable | `BACKUP_SQL_DATABASE` | `TimeTrackerDb` |

`AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` are reused from the deploy setup.

### Verifying

Run the workflow manually from **Actions → Database Backup → Run workflow**. A `.bacpac` artifact will appear on the run within a few minutes. Check that the firewall rule `github-actions-backup` is absent from the SQL server after the run completes (the cleanup step removes it unconditionally).

---

## Free tier limits

| Resource | Plan | Limit | When exceeded |
|----------|------|-------|---------------|
| App Service | F1 | 60 CPU min/day, 1 GB RAM, sleeps after 20 min idle | Throttled, no charge |
| Azure SQL | Free offer | 32 GB data, 32 GB backup | Auto-pauses, no charge |
| GitHub Actions | Free | 2,000 min/month, 500 MB artifact storage | Blocked until next cycle |
