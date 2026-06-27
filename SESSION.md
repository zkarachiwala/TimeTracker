# Session handoff — 2026-06-27

## Completed this session
- ✅ **#207 Upgrade dev container SQL Server 2022 → 2025** — PR open (branch `chore/upgrade-sqlserver-2025`)
- ✅ Created 16 DP-800 exam learning issues (#208–#223) — all added to project board at Medium priority

## DP-800 exam — learning issues (study guide order)

Exam date: **6 August 2026**. All issues are fully local: SQL Server 2025 Developer + Ollama.

### Design and develop database solutions (35–40%)

#### Design and implement database objects
| # | Title |
|---|-------|
| #208 | DP-800: Temporal table on TimeEntries (specialized tables) |
| #209 | DP-800: Native JSON column and index on Projects (JSON columns) |

#### Implement programmability objects
| # | Title |
|---|-------|
| #210 | DP-800: TVF and stored procedure for time reporting (programmability objects) |

#### Write advanced T-SQL
| # | Title |
|---|-------|
| #211 | DP-800: CTEs and window functions for the reports page (advanced T-SQL) |
| #212 | DP-800: JSON query functions on Projects metadata (JSON functions) |
| #213 | DP-800: Regular expressions on time entry descriptions (regex functions) |
| #214 | DP-800: Fuzzy matching for project and entry search (fuzzy string functions) |

#### AI-assisted tools
| # | Title |
|---|-------|
| #215 | DP-800: GitHub Copilot instruction file and SQL MCP Server (AI-assisted tools) |

### Secure, optimize, and deploy database solutions (35–40%)

#### Data security
| # | Title |
|---|-------|
| #216 | DP-800: Dynamic Data Masking on TimeEntry descriptions (data security) |

#### Performance optimization
| # | Title |
|---|-------|
| #217 | DP-800: Query Store and DMV investigation (performance optimization) |

#### CI/CD with SQL Database Projects
| # | Title |
|---|-------|
| #218 | DP-800: SDK-style SQL Database Project with GitHub Actions CI (CI/CD) |

#### Azure services integration
| # | Title |
|---|-------|
| #219 | DP-800: Data API Builder REST and GraphQL endpoints (Azure services integration) |

### Implement AI capabilities (25–30%)

#### Models and embeddings
| # | Title |
|---|-------|
| #220 | DP-800: External model and embeddings with Ollama (models and embeddings) |

#### Intelligent search
| # | Title |
|---|-------|
| #221 | DP-800: Full-text search on time entry descriptions (intelligent search) |
| #222 | DP-800: Vector search and hybrid search with RRF (intelligent search) |

#### RAG
| # | Title |
|---|-------|
| #223 | DP-800: Natural language weekly summary via sp_invoke_external_rest_endpoint (RAG) |

## Before starting any DP-800 issue
1. `docker compose down -v` — wipe the 2022 volume
2. Merge PR #207 (SQL Server 2025 upgrade) and pull main
3. Reopen the dev container — `postCreateCommand` re-runs migrations
4. For issues #213, #214, #222: `ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON`
5. For issue #223: `EXEC sp_configure 'external rest endpoint enabled', 1; RECONFIGURE;`
6. Ollama models: `ollama pull nomic-embed-text` (#220, #222) and `ollama pull llama3.2` (#223)

## Dev container — how it works now
- Open VS Code in the repo → click "Reopen in Container" from the `><` menu (bottom-left)
- App starts automatically at `http://localhost:5019`
- Google OAuth credentials come from WSL User Secrets (mounted read-only into the container)
- Docker panel in VS Code sidebar manages containers (start, stop, logs)
- To seed mock data: log in → POST `/api/dev/seed` via Swagger at `/swagger`
- Data persists in named volume — only wiped by `docker compose down -v`

## Known gotcha: build artifacts
The container writes `obj/bin` files. Running tests from the host while the container is active fails with MSB3492 errors. Fix: stop the container first, or clean via Docker:
```bash
docker run --rm -v $(pwd):/workspace alpine sh -c "rm -rf /workspace/TimeTracker.*/obj /workspace/TimeTracker.*/bin"
```

## Standard test commands
**Before every PR:**
```bash
PLAYWRIGHT_WRITE_TESTS=true BROWSER= dotnet test TimeTracker.sln --logger "console;verbosity=normal" --blame-hang-timeout 60s
```
**Fast (no Docker or browser):**
```bash
dotnet test TimeTracker.Tests --filter "Category!=Container" && dotnet test TimeTracker.ComponentTests
```
**Container tests only (requires Docker):**
```bash
dotnet test TimeTracker.Tests --filter "Category=Container"
```
**Showcase smoke tests:**
```bash
BROWSER= dotnet test TimeTracker.Playwright --filter "FullyQualifiedName~ShowcaseTests" --logger "console;verbosity=normal" --blame-hang-timeout 60s
```

## Backlog (non-DP-800)

### 🟡 Medium
| # | Title |
|---|-------|
| #166 | CSV export for time entries |
| #167 | Project budget tracking |
| #121 | Add distributed tracing and APM via OpenTelemetry and Grafana Cloud |

### 🟢 Low
| # | Title |
|---|-------|
| #168 | Duplicate time entry |
| #169 | Tags for time entries |
| #170 | Time rounding rules |
| #36  | Invoice export — uninvoiced entries per client for Zoho Books |
| #108 | Run Lighthouse audit and address findings |
| #163 | Define Azure infrastructure as code with Bicep |
| #102 | Add email/password login fallback and TOTP MFA |
| #96  | Add staging environment (requires paid tier upgrade) |

## Active tech debt
| # | Item | ADR |
|---|------|-----|
| TD1 | Global WASM rendering | D001, D003 |
| TD2 | F1: single instance, no slots | D003 |
| TD4 | Azure SQL free (auto-pause) | D003 |
| TD6 | No staging environment | D016 |
| TD17 | `unsafe-inline` CSP (MudBlazor) | D002 |
| TD21 | Cloudflare free plan | D017 |
| TD23 | No APM / distributed tracing | D019 |
| TD25 | Award rate jurisdiction hardcoded to national AU | D025 |

## How to resume
```bash
cd /home/zkarachiwala/repos/TimeTracker
git status
cat SESSION.md
```

---
*Updated 2026-06-27. SQL Server 2025 upgrade PR open (#207). 16 DP-800 learning issues created (#208–#223). Next: merge #207, then start DP-800 issues in study guide order from #208.*
