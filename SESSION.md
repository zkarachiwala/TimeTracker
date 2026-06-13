# Session handoff — 2026-06-14

## Current state
- Branch: `main`, clean
- PRs #114 (#111), #117 (#116), #118 (#100) all merged this session
- New issues created: #115 (cancellation tokens), #116 (pagination cap)

## Completed this session
- ✅ #111 Program.cs refactor (PR #114)
- ✅ #116 Server-side pagination cap + timer dispose fix (PR #117)
- ✅ #100 Global rate limiting covering all endpoints (PR #118)
- ✅ D018 added to decisions.md, architecture.md updated

## Plan — remaining medium priority (work in order)

### Next up — Step C (before moving on)
- **#115** 🟢 Cancellation tokens — thread `CancellationToken` through all service methods and EF Core calls
  - All service interfaces + implementations (TimeEntries, Projects, Clients, Auth)
  - All EF Core async calls (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`, etc.)
  - Largest refactor of the defence-in-depth batch; do before Step 3

### Step 3 — Session revocation
- **#103** 🟡 Session revocation via SecurityStamp

### Step 4 — Structured logging & monitoring
- **#97** 🟡 Structured logging (Serilog), APM (Application Insights), uptime monitoring (UptimeRobot), `/health` endpoint

### Step 5 — Security hardening
- **#101** 🟡 SQL Server Row-Level Security
- **#104** 🟡 Automated database backup export

## Backlog (low priority)
- **#95** 🟢 Database-backed user management
- **#96** 🟢 Staging environment (requires paid tier upgrade)
- **#102** 🟢 Email/password fallback + TOTP MFA
- **#115** 🟢 Cancellation tokens (defence-in-depth, Step C)

## Active tech debt (genuine constraints, no action until paid tier)
| # | Item | ADR |
|---|------|-----|
| TD1 | Global WASM rendering | D001, D003 |
| TD2 | F1: single instance, no slots | D003 |
| TD4 | Azure SQL free (auto-pause) | D003 |
| TD6 | No staging environment | D016 |
| TD17 | `unsafe-inline` CSP (MudBlazor) | D002 |
| TD21 | Cloudflare free plan | D017 |

## How to resume
```bash
cd /home/zkarachiwala/repos/TimeTracker
git status
cat SESSION.md
```

---
*Updated 2026-06-14. Next: #115 cancellation tokens (Step C), then Step 3 session revocation.*
