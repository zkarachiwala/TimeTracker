# Azure Deployment Setup

One-time steps to provision and wire up the Azure resources for TimeTracker.
After this guide is complete, every push to `main` that passes CI will deploy automatically.

---

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed and logged in (`az login`)
- An Azure subscription (free tier is sufficient)
- A Google Cloud project with OAuth credentials (see `docs/google-oauth-setup.md`)

---

## 1. Create the resource group

```bash
az group create --name timetracker-rg --location australiaeast
```

---

## 2. Create Azure SQL Database (free offer)

```bash
# Create the logical server
az sql server create \
  --resource-group timetracker-rg \
  --name timetracker-sql \
  --location australiaeast \
  --enable-ad-only-auth \
  --external-admin-principal-type User \
  --external-admin-name <your-azure-ad-email> \
  --external-admin-sid <your-azure-ad-object-id>

# Create the free-tier database (32 GB, automatic backups)
az sql db create \
  --resource-group timetracker-rg \
  --server timetracker-sql \
  --name TimeTrackerDb \
  --edition GeneralPurpose \
  --compute-model Serverless \
  --family Gen5 \
  --capacity 2 \
  --use-free-limit \
  --free-limit-exhaustion-behavior AutoPause

# Allow Azure services to reach the server (App Service uses this)
az sql server firewall-rule create \
  --resource-group timetracker-rg \
  --server timetracker-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

> **Note:** `--enable-ad-only-auth` disables SQL username/password authentication entirely.
> Only Azure AD / Managed Identity accounts can connect.

---

## 3. Create the App Service (F1 free plan)

```bash
# Create the free App Service plan
az appservice plan create \
  --resource-group timetracker-rg \
  --name timetracker-plan \
  --sku F1 \
  --is-linux

# Create the web app (.NET 10)
az webapp create \
  --resource-group timetracker-rg \
  --plan timetracker-plan \
  --name <your-app-name> \
  --runtime "DOTNETCORE:10.0"
```

Replace `<your-app-name>` with a globally unique name (e.g. `timetracker-zak`).
This becomes the default URL: `https://<your-app-name>.azurewebsites.net`.

---

## 4. Enable system-assigned Managed Identity

```bash
az webapp identity assign \
  --resource-group timetracker-rg \
  --name <your-app-name>
```

Copy the `principalId` from the output — you need it in the next step.

---

## 5. Grant the Managed Identity access to Azure SQL

```bash
# Connect to the database as your AD admin and run:
az sql db show-connection-string \
  --server timetracker-sql \
  --name TimeTrackerDb \
  --client ado.net
```

Then connect to the database (e.g. via Azure Data Studio with your AD account) and run:

```sql
CREATE USER [<your-app-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<your-app-name>];
ALTER ROLE db_datawriter ADD MEMBER [<your-app-name>];
```

Replace `<your-app-name>` with your App Service name — Azure creates the SQL user from the Managed Identity automatically.

---

## 6. Configure App Service environment variables

Set all configuration values in App Service. These override `appsettings.json` at runtime.

```bash
APP=<your-app-name>
RG=timetracker-rg
SERVER=timetracker-sql

CONN="Server=${SERVER}.database.windows.net;Database=TimeTrackerDb;Authentication=Active Directory Managed Identity;Encrypt=True"

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
    "Authentication__Google__ClientId=<google-client-id>" \
    "Authentication__Google__ClientSecret=<google-client-secret>" \
    "Authentication__AllowedEmails__0=zak@dzk.com.au" \
    "ASPNETCORE_ENVIRONMENT=Production"
```

The connection string uses `Authentication=Active Directory Managed Identity` — no username or password anywhere.

---

## 7. Add GitHub Actions secrets and variables

In your GitHub repository go to **Settings → Secrets and variables → Actions**.

### Download the publish profile

```bash
az webapp deployment list-publishing-profiles \
  --resource-group timetracker-rg \
  --name <your-app-name> \
  --xml
```

Copy the entire XML output.

### Add to GitHub

| Type | Name | Value |
|------|------|-------|
| Secret | `AZURE_WEBAPP_PUBLISH_PROFILE` | The publish profile XML |
| Variable | `AZURE_WEBAPP_NAME` | `<your-app-name>` |

---

## 8. First deployment

Push any commit to `main` (or re-run the CI workflow). Once CI passes, the Deploy workflow fires automatically and deploys to Azure.

The app applies EF Core migrations on startup — no manual `dotnet ef database update` needed.

Check the deployment log at:
`https://github.com/zkarachiwala/TimeTracker/actions`

---

## Free tier limits

| Resource | Plan | Limit | When exceeded |
|----------|------|-------|---------------|
| App Service | F1 | 60 CPU min/day, 1 GB RAM, sleeps after 20 min idle | Throttled, no charge |
| Azure SQL | Free offer | 32 GB data, 32 GB backup | Auto-pauses, no charge |
