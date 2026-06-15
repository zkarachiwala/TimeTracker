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

## Refreshing the Playwright auth state

The Playwright E2E suite authenticates by replaying a stored browser session. That session is encoded as the `PLAYWRIGHT_AUTH_STATE_B64` GitHub Actions secret. The session cookie has a **1-day expiration** — once it expires, all authenticated tests will fail with a timeout waiting for `.tt-fab button`.

### When to refresh

- After the auth cookie expires (authenticated tests all fail with 30 s timeout on `.tt-fab button`)
- After a full sign-out or Identity schema change

### Steps

**1 — Start the app locally in Development mode**

```bash
cd TimeTracker.Web && dotnet run
```

The dev login endpoint (`/api/dev/login`) is only available in Development mode. The app must be running before step 2.

**2 — Capture fresh auth state**

```bash
BROWSER= PLAYWRIGHT_BASE_URL=https://localhost:7006 \
  dotnet test TimeTracker.Playwright \
  --filter "FullyQualifiedName~CaptureAuthState" \
  --logger "console;verbosity=normal"
```

This navigates to `/api/dev/login`, signs in as the first Admin user, waits for WASM to hydrate, and saves the browser session to:

```
TimeTracker.Playwright/bin/Debug/net10.0/playwright/.auth/user.json
```

**3 — Update the GitHub secret**

```bash
cat TimeTracker.Playwright/bin/Debug/net10.0/playwright/.auth/user.json \
  | base64 -w 0 \
  | gh secret set PLAYWRIGHT_AUTH_STATE_B64
```

No copy/paste required — `gh` reads from stdin and updates the secret directly.

**4 — Verify**

Merge any change to `main` (or re-run the Deploy workflow). The Playwright job runs post-deploy and should show all authenticated tests passing.

---

## Free tier limits

| Resource | Plan | Limit | When exceeded |
|----------|------|-------|---------------|
| App Service | F1 | 60 CPU min/day, 1 GB RAM, sleeps after 20 min idle | Throttled, no charge |
| Azure SQL | Free offer | 32 GB data, 32 GB backup | Auto-pauses, no charge |
