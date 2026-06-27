# Session handoff — 2026-06-27

## Completed this session
- ✅ **#207 Upgrade dev container SQL Server 2022 → 2025** — merged
- ✅ Created 16 DP-800 exam learning issues (#208–#223) — all added to project board at Medium priority
- ✅ Created AZ-400 GitHub label
- ✅ Created 27 AZ-400 exam learning issues (#225–#251) — all added to project board at Medium priority
  - #225: Budget alert (tagged DP-800 + AZ-400 — do this first before incurring any Azure costs)
  - #226–#229: Processes & communications (wiki, release notes, Teams webhook, DORA metrics)
  - #230–#232: Source control (branch protection, SemVer, git recovery)
  - #233–#245: Build & release pipelines (packages, coverage, load test, environments, templates, runner, feature flags, migrations, blue-green, Bicep, ADE, pipeline health, retention)
  - #246–#249: Security & compliance (OIDC, Key Vault, CodeQL, Managed Identity)
  - #250–#251: Instrumentation (App Insights, KQL)
- ✅ Created Azure DevOps label and 5 Azure DevOps issues (#252–#256) — all tagged AZ-400 + Azure DevOps
  - #252: Azure Pipelines YAML CI (parallel to GitHub Actions)
  - #253: Azure Boards — work items, sprints, AB# commit linking
  - #254: Azure Repos — branch policies, build validation
  - #255: Azure Artifacts — NuGet feed with upstream sources
  - #256: Azure Pipelines environments, approvals, deployment jobs
- ✅ Reformatted all 27 AZ-400 issues (#225–#251) to match DP-800 format (blockquote exam objective, Your task, Explore with hands-on commands, Exam checkpoint)

## DP-800 exam — learning issues (study guide order)

All issues are fully local: SQL Server 2025 Developer + Ollama.

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

## AZ-400 exam — learning issues (study guide order)

All issues follow the learning exercise format: read-first links, YOUR task (you do the work), how to verify, and exam checkpoint questions.

| # | Title | Skill area |
|---|-------|-----------|
| #225 | Budget alert (DP-800 + AZ-400) | Prerequisite — do first |
| #226 | Mermaid architecture diagrams in wiki | Processes & comms |
| #227 | Automated release notes from git history | Processes & comms |
| #228 | GitHub → Teams webhook for CI/deploy events | Processes & comms |
| #229 | DORA metrics dashboard | Processes & comms |
| #230 | Branch protection rules and CODEOWNERS | Source control |
| #231 | SemVer tagging strategy and automated version bump | Source control |
| #232 | Git data recovery (reflog, bisect, revert, filter-repo) | Source control |
| #233 | Publish TimeTracker.Contracts to GitHub Packages | Pipelines |
| #234 | Code coverage with Coverlet and CI quality gate | Pipelines |
| #235 | k6 load test in CI pipeline | Pipelines |
| #236 | Multi-stage pipeline with GitHub Environments + approval gates | Pipelines |
| #237 | Reusable YAML workflow templates | Pipelines |
| #238 | Self-hosted GitHub Actions runner in Docker | Pipelines |
| #239 | Feature flags with Azure App Configuration | Pipelines |
| #240 | EF Core migrations as explicit pipeline step | Pipelines |
| #241 | Blue-green deployment docs + feature flag simulation | Pipelines |
| #242 | Bicep templates for full Azure infrastructure | Pipelines / IaC |
| #243 | Azure Deployment Environments for PR environments | Pipelines / IaC |
| #244 | Pipeline health (failure rate, flaky tests) | Pipelines |
| #245 | Pipeline artifact retention policy | Pipelines |
| #246 | OIDC workload identity federation (replace PAT) | Security |
| #247 | Azure Key Vault for secrets | Security |
| #248 | CodeQL and GitHub Advanced Security | Security |
| #249 | Managed Identity for App Service → Azure SQL | Security |
| #250 | Application Insights instrumentation | Instrumentation |
| #251 | KQL queries for operational metrics | Instrumentation |

---
*Updated 2026-06-27. SQL Server 2025 upgrade merged (#207). 16 DP-800 learning issues created (#208–#223). 27 AZ-400 learning issues (#225–#251) + 5 Azure DevOps issues (#252–#256) created. Next: start DP-800 from #208. For AZ-400, start with #225 (budget alert) before incurring any Azure costs.*
