# Session handoff — 2026-06-13

## Current state
- Branch: `chore/tech-debt-review`, uncommitted changes ready to commit
- All CI green on main

## What was completed this session

### Tech debt register overhaul
- Reduced active items from 22 to 6 genuine tier/cost-constrained items (TD1, TD2, TD4, TD6, TD17, TD21)
- Retired/merged all items that were not true debt: TD3→TD2, TD5, TD7→TD6, TD8, TD9, TD10, TD11, TD12, TD13, TD14, TD15, TD16, TD18, TD19, TD20, TD22→TD6
- Every active item now links to a backing ADR
- Added D017 (Cloudflare free plan decision)

### GitHub issues created (#93–#105)
All added to Timetracker project board with Priority and labels:
- **#93** 🔴 — EF Core connection pool limits
- **#94** 🔴 — SAST/dependency scanning
- **#98** 🔴 — Global exception handler (RFC 7807)
- **#99** 🔴 — Restrict AllowedHosts
- **#105** 🔴 — Managed Identity for Azure SQL (eliminates DbUser/DbPassword)
- **#97** 🟡 — Structured logging, APM, uptime monitoring, `/health` endpoint
- **#100** 🟡 — Rate limiting on all mutating endpoints
- **#101** 🟡 — SQL Server Row-Level Security
- **#103** 🟡 — Session revocation via SecurityStamp
- **#104** 🟡 — Automated database backup export
- **#95** 🟢 — Database-backed user management (future)
- **#96** 🟢 — Staging environment (requires paid tier)
- **#102** 🟢 — Email/password fallback + TOTP MFA (future)

### Other changes
- `CLAUDE.md` — added project dual-purpose context (practical app + learning exercise); added rule to always add issues to Timetracker project board with Priority and labels
- `README.md` — added ClockLogo.png as header image
- `docs/decisions.md` — D002 updated with explicit CSS injection security explanation; D017 added for Cloudflare
- Azure config hardened: HTTPS only, TLS 1.2, FTPS only, remote debug off, local IP firewall rule removed from Azure SQL

## Active tech debt (genuine tier constraints)
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
gh issue list --state open
```

---
*Updated 2026-06-13. Branch: chore/tech-debt-review — pending PR.*
