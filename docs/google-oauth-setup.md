# Google OAuth Setup — Google Cloud Console

Steps to create OAuth 2.0 credentials for TimeTracker.

---

## 1. Create a project

1. Go to [https://console.cloud.google.com](https://console.cloud.google.com)
2. Click the project dropdown at the top → **New Project**
3. Name it `TimeTracker` (or anything recognisable) → **Create**
4. Make sure the new project is selected in the dropdown before continuing

---

## 2. Configure the OAuth consent screen

1. In the left sidebar go to **APIs & Services → OAuth consent screen**
2. Choose **External** → **Create**
   - "External" is required even for personal use unless you have a Google Workspace org
3. Fill in the required fields:
   - **App name:** `TimeTracker`
   - **User support email:** your Gmail address
   - **Developer contact email:** your Gmail address
4. Click **Save and Continue** through the Scopes and Test Users screens (no changes needed)
5. On the Summary screen click **Back to Dashboard**
6. Under **Publishing status** click **Publish App** → **Confirm**
   - This removes the "unverified app" warning screen during login

---

## 3. Create OAuth 2.0 credentials

1. Go to **APIs & Services → Credentials**
2. Click **+ Create Credentials → OAuth client ID**
3. **Application type:** Web application
4. **Name:** `TimeTracker Web`
5. Under **Authorised redirect URIs** click **+ Add URI** and add:

   **Local development:**
   ```
   https://localhost:7006/signin-google
   ```

   **Production (add once you have the Azure URL):**
   ```
   https://<your-app-service-name>.azurewebsites.net/signin-google
   ```

6. Click **Create**
7. A dialog shows your **Client ID** and **Client Secret** — copy both now (you can retrieve them again later from the Credentials page)

---

## 4. Store credentials locally

Run these commands from the `TimeTracker.Web` directory:

```bash
cd TimeTracker.Web
dotnet user-secrets set "Authentication:Google:ClientId" "<your-client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-client-secret>"
dotnet user-secrets set "Authentication:AllowedEmails:0" "zak@dzk.com.au"
```

---

## 5. Store credentials in Azure (production)

Once the App Service exists (Phase 7), add these as Application Settings:

| Name | Value |
|------|-------|
| `Authentication__Google__ClientId` | your client ID |
| `Authentication__Google__ClientSecret` | your client secret |
| `Authentication__AllowedEmails__0` | zak@dzk.com.au |

> Azure uses `__` as the hierarchy separator instead of `:`.

---

## Notes

- The `/signin-google` callback path is the ASP.NET Core default. It is set by the `AddGoogle()` middleware and must match exactly what is registered above.
- If you later add Entra as a provider, its redirect URI follows the same pattern: `/signin-microsoft` (or whatever `CallbackPath` is configured).
- Client ID is not secret and will appear in browser requests. Client Secret must never be committed to source control.
