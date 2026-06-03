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

## Step 7 — Add GitHub Actions secrets and variables

### Get the publish profile

```bash
az webapp deployment list-publishing-profiles \
  --resource-group $RG \
  --name $APP \
  --xml
```

Copy the entire XML output.

### Add to GitHub

In your repository go to **Settings → Secrets and variables → Actions** and add:

| Type | Name | Value |
|------|------|-------|
| Secret | `AZURE_WEBAPP_PUBLISH_PROFILE` | The XML from the command above |
| Variable | `AZURE_WEBAPP_NAME` | The value of `APP` (e.g. `timetracker-zak`) |

---

## Step 8 — First deployment

Merge PR #44 (or push any commit to `main`). Once CI passes, the Deploy workflow fires and deploys the app. Migrations run automatically on startup.

Check progress at: `https://github.com/zkarachiwala/TimeTracker/actions`

Your app will be live at: `https://${APP}.azurewebsites.net`

---

## Free tier limits

| Resource | Plan | Limit | When exceeded |
|----------|------|-------|---------------|
| App Service | F1 | 60 CPU min/day, 1 GB RAM, sleeps after 20 min idle | Throttled, no charge |
| Azure SQL | Free offer | 32 GB data, 32 GB backup | Auto-pauses, no charge |
