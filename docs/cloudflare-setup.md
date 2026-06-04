# Cloudflare Custom Domain Setup

One-time steps to route `timetracker.dzk.com.au` through Cloudflare to the existing App Service.
A Cloudflare Worker handles the host header rewrite that App Service F1 requires.

---

## Why a Worker is needed

App Service uses the `Host` header to route requests to the correct app. F1 does not support
custom domain bindings, so it rejects any request where `Host` is not `*.azurewebsites.net`.
When Cloudflare proxies `timetracker.dzk.com.au`, it forwards `Host: timetracker.dzk.com.au`
to the origin — which App Service 404s. The Worker rewrites the outbound request URL to
`timetracker-zak.azurewebsites.net`, causing the Workers runtime to set the Host header
correctly without any paid Cloudflare plan features.

---

## Prerequisites

- `dzk.com.au` nameservers pointing at Cloudflare
- Access to [Google Cloud Console](https://console.cloud.google.com) to update the OAuth redirect URI
- Access to the Azure Portal to add one App Service setting

---

## Step 1 — Add a DNS record in Cloudflare

In the Cloudflare dashboard → `dzk.com.au` zone → **DNS** → **Add record**:

| Field | Value |
|-------|-------|
| Type | `CNAME` |
| Name | `timetracker` |
| Target | `timetracker-zak.azurewebsites.net` |
| Proxy status | **Proxied** (orange cloud — must be on) |
| TTL | Auto |

---

## Step 2 — Set SSL/TLS mode to Full

In the Cloudflare dashboard → **SSL/TLS** → **Overview**, select **Full**.

Cloudflare terminates TLS from the browser, then connects to App Service over HTTPS using
the `*.azurewebsites.net` certificate that Azure manages automatically.

---

## Step 3 — Deploy the Cloudflare Worker

The Worker script is at `workers/proxy.js` in this repository.

1. Go to [dash.cloudflare.com](https://dash.cloudflare.com) → **Workers & Pages** → **Create** → **Create Worker**
2. Name it `timetracker-proxy`, click **Deploy**
3. Click **Edit code**, replace all content with the contents of `workers/proxy.js`, click **Deploy**

### Attach the Worker to the custom domain

4. Go to **Workers & Pages** → select `timetracker-proxy` → **Settings** → **Domains & Routes** → **Add** → **Route**
5. Set:

| Field | Value |
|-------|-------|
| Zone | `dzk.com.au` |
| Route pattern | `timetracker.dzk.com.au/*` |

6. Click **Add route**

The Worker now intercepts all traffic to `timetracker.dzk.com.au` and proxies it to App Service
with the correct Host header. WebSocket connections (Blazor SignalR) are also proxied.

> **Free plan limits:** 100,000 requests/day. CPU time per request does not count network
> wait — a simple proxy uses well under the 10 ms CPU limit.

---

## Step 4 — Enable forwarded headers in App Service

Tells ASP.NET Core to trust `X-Forwarded-Proto: https` from Cloudflare so that HTTPS
redirect and `SameSite=Strict` cookies behave correctly.

In the Azure Portal → **App Services** → `timetracker-zak` → **Environment variables** → **Add**:

| Name | Value |
|------|-------|
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` |

Save and wait for the app to restart (~30 seconds).

Or via Azure CLI:

```bash
az webapp config appsettings set \
  --resource-group timetracker-rg \
  --name timetracker-zak \
  --settings ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

---

## Step 5 — Update Google OAuth redirect URI

1. Go to [console.cloud.google.com](https://console.cloud.google.com) → select the TimeTracker project
2. **APIs & Services** → **Credentials** → click the OAuth 2.0 Client ID
3. Under **Authorised redirect URIs** add:

```
https://timetracker.dzk.com.au/signin-google
```

Keep the existing `https://timetracker-zak.azurewebsites.net/signin-google` entry.

4. Click **Save** — changes propagate within a few minutes

---

## Step 6 — Verify

```bash
# Cloudflare should be in the response headers
curl -I https://timetracker.dzk.com.au
# Expect: HTTP/2 200 (or 302 to /login)
# Expect: server: cloudflare
```

Then open `https://timetracker.dzk.com.au` in a browser and complete a Google login to
confirm the OAuth callback works end to end.
