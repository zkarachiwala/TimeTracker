# UptimeRobot Setup — Uptime Monitoring for TimeTracker

UptimeRobot pings the `/health` endpoint every 5 minutes from external servers and emails you if the app goes down. Free tier is sufficient.

---

## 1. Create an account

1. Go to [https://uptimerobot.com](https://uptimerobot.com)
2. Click **Register for FREE**
3. Fill in your name, email (`zak@dzk.com.au`), and a password
4. Check your inbox and click the verification link
5. Log in — you will land on the **Dashboard**

---

## 2. Add an alert contact (so you actually get notified)

Before creating a monitor, make sure an alert contact is configured.

1. In the left sidebar click **Alert Contacts**
2. Click **Add Alert Contact**
3. Set:
   - **Alert Contact Type:** E-mail
   - **Friendly Name:** Personal email (or whatever you like)
   - **E-mail:** `zak@dzk.com.au`
4. Click **Create Alert Contact**
5. Check your inbox for a verification email and click the link — UptimeRobot will not send alerts to an unverified address

---

## 3. Create the monitor

1. In the left sidebar click **Monitors**
2. Click **+ Add New Monitor**
3. Fill in the form:

   | Field | Value |
   |-------|-------|
   | Monitor Type | **HTTP(s)** |
   | Friendly Name | `TimeTracker - Health` |
   | URL (or IP) | `https://timetracker-zak.azurewebsites.net/health` |
   | Monitoring Interval | **5 minutes** (free tier minimum) |
   | Monitor Timeout | 30 seconds |

4. Scroll down to **Alert Contacts To Notify** and tick the contact you created in step 2
5. Click **Create Monitor**

---

## 4. Verify it's working

1. You will see the monitor appear on the dashboard with a green **Up** badge within a few minutes
2. Click the monitor name to see the response time graph and uptime percentage
3. The `/health` endpoint returns JSON like:
   ```json
   {"status":"Healthy","checks":[{"name":"app-db","status":"Healthy"},{"name":"identity-db","status":"Healthy"}]}
   ```
   UptimeRobot only checks for HTTP 200 — it does not parse the JSON body

---

## 5. (Optional) Create a public status page

UptimeRobot can generate a free public status page (e.g. `stats.uptimerobot.com/xxxxx`) useful for checking status without logging in.

1. In the left sidebar click **Status Pages**
2. Click **Create Status Page**
3. Set a friendly name (e.g. `TimeTracker Status`) and add the health monitor to it
4. Save — UptimeRobot generates a public URL you can bookmark

---

## What happens when the app goes down

- UptimeRobot detects the failure after the next 5-minute check
- It sends an email alert immediately to your registered contact
- When the app recovers, it sends a second email confirming recovery
- The dashboard shows the downtime duration and timestamp

---

## What the /health endpoint checks

`GET /health` is a **liveness check only** — it confirms the app process is running and returns `{"status":"Healthy"}` with HTTP 200. It deliberately does **not** ping the database.

**Why no DB check?** The Azure SQL free tier includes 100,000 vCore seconds per month. At the serverless minimum of 0.5 vCores, that's ~55 hours of active uptime. A database ping every 5 minutes prevents Azure SQL from auto-pausing, which would exhaust the free allowance in ~2 days and either pause the DB until the next billing month or start incurring charges.

**For manual DB diagnostics**, `GET /health/detail` runs the full connectivity check against both DbContexts and requires authentication. Use this to confirm the database is reachable when troubleshooting, but never point an external monitor at it.
