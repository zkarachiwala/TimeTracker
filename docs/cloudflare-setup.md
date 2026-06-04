# Cloudflare Custom Domain Setup

One-time steps to route `timetracker.dzk.com.au` through Cloudflare to the existing App Service.
After this guide is complete the custom domain is live with free managed SSL — no Azure changes required.

---

## Prerequisites

- `dzk.com.au` nameservers pointing at Cloudflare (i.e. `dzk.com.au` is already a Cloudflare zone)
- Access to [Google Cloud Console](https://console.cloud.google.com) to update the OAuth redirect URI
- Access to the Azure Portal to add one App Service setting

---

## Step 1 — Add a DNS record in Cloudflare

In the Cloudflare dashboard, open the `dzk.com.au` zone → **DNS** → **Add record**:

| Field | Value |
|-------|-------|
| Type | `CNAME` |
| Name | `timetracker` |
| Target | `timetracker-zak.azurewebsites.net` |
| Proxy status | **Proxied** (orange cloud — must be on) |
| TTL | Auto |

The orange cloud is what routes traffic through Cloudflare and provides TLS termination. Do not set it to DNS-only (grey cloud).

---

## Step 2 — Set SSL/TLS mode to Full

In the Cloudflare dashboard → **SSL/TLS** → **Overview**, select **Full**.

| Mode | What it means |
|------|---------------|
| Flexible | Cloudflare → App Service over HTTP — do not use, causes header mismatch |
| **Full** | Cloudflare → App Service over HTTPS — correct choice |
| Full (Strict) | Same as Full but validates App Service cert — also works, optional |

Full is correct here: Cloudflare terminates TLS from the browser, then connects to App Service over HTTPS using the `*.azurewebsites.net` certificate (which Azure manages automatically).

---

## Step 3 — Enable forwarded headers in App Service

This tells ASP.NET Core to trust the `X-Forwarded-Proto: https` header that Cloudflare sends, so HTTPS redirect and cookies behave correctly.

In the Azure Portal → **App Services** → `timetracker-zak` → **Environment variables** → **Add**:

| Name | Value |
|------|-------|
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` |

Save and wait for the app to restart (~30 seconds).

Alternatively via the Azure CLI:

```bash
az webapp config appsettings set \
  --resource-group timetracker-rg \
  --name timetracker-zak \
  --settings ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

---

## Step 4 — Update Google OAuth redirect URI

Google rejects OAuth callbacks to URLs it has not seen before. You need to add the Cloudflare domain alongside the existing App Service URL.

1. Go to [console.cloud.google.com](https://console.cloud.google.com)
2. Select the project used for TimeTracker
3. Navigate to **APIs & Services** → **Credentials**
4. Click the OAuth 2.0 Client ID used by TimeTracker
5. Under **Authorised redirect URIs**, add:

```
https://timetracker.dzk.com.au/signin-google
```

Keep the existing `https://timetracker-zak.azurewebsites.net/signin-google` entry — removing it breaks direct App Service access.

6. Click **Save**

Changes propagate within a few minutes.

---

## Step 5 — Verify

Once DNS propagates (usually under 5 minutes with Cloudflare):

```bash
# Should resolve via Cloudflare
curl -I https://timetracker.dzk.com.au
# Expect: HTTP/2 200 (or 302 to /login)
# Check for: server: cloudflare
```

Then open `https://timetracker.dzk.com.au` in a browser and complete a Google login to confirm the OAuth callback works end to end.

---

## What Cloudflare provides

| Feature | Detail |
|---------|--------|
| TLS certificate | Auto-provisioned, auto-renewed — no action required |
| Origin IP hiding | App Service URL not exposed in DNS lookups |
| DDoS mitigation | Basic protection included on free plan |
| Cost | Free plan, hard-capped — no overage possible |
