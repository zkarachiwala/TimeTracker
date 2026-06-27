# DP-800 Udemy Course Map

> **Course credit:** This map is based on the Udemy course
> [DP-800 Exam Prep: Microsoft SQL Server AI Developer](https://www.udemy.com/course/dp-800-exam-prep-microsoft-sql-server-ai-developer/)
> — 29 sections · 170 lectures · 19h 6m.
> All hands-on GitHub issues in this repository reference lectures from that course.
> Course content belongs to its respective instructor and Udemy. This map is a personal study aide only.

## Legend
- ✅  Has existing TimeTracker DP-800 issue
- 📖  Study only — basic SQL/known material, no hands-on issue needed
- 🔵  Microsoft Fabric only — cannot run locally on SQL Server 2025

---

## SECTION 1 — Introduction
| Lecture | Notes |
|---|---|
| Welcome to Udemy | 📖 |
| How to get the best out of your course | 📖 |
| Subtitles | 📖 |

---

## SECTION 2 — Setup & Tooling
| Lecture | Issue | Notes |
|---|---|---|
| Getting a free Azure trial | 📖 | See also #225 (budget alert — do this first) |
| Creating Azure SQL Database with sample data | 📖 | Dev container uses SQL Server 2025 locally |
| Installing SSMS | 📖 | Already set up |
| Using SSMS + connecting to Azure SQL | 📖 | Already set up |

---

## SECTION 3 — Basic T-SQL (SELECT fundamentals)
| Lecture | Issue | Notes |
|---|---|---|
| SELECT and FROM — Part 1 & 2 | 📖 | Foundational — already known |
| WHERE clause (numbers, strings, dates, LIKE, NULL, multiple conditions) | 📖 | |
| GROUP BY and aggregations | 📖 | |
| HAVING clause | 📖 | |
| ORDER BY clause | 📖 | |
| Using all 6 principal clauses together | 📖 | |

---

## SECTION 4 — Data Types
| Lecture | Issue | Notes |
|---|---|---|
| Number data types (integer, non-integer) | 📖 | |
| Dealing with NULLs using functions | 📖 | |
| String data types + string functions | 📖 | |
| Date data types and functions | 📖 | |
| Other data types | 📖 | |

---

## SECTION 5 — DDL: Tables and Constraints
| Lecture | Issue | Notes |
|---|---|---|
| Creating tables | 📖 | |
| ALTER and DROP tables | 📖 | |
| INSERT, DELETE, UPDATE | 📖 | |
| Additional ways to create tables (SELECT INTO, etc.) | 📖 | |
| UNIQUE constraint (4c) | 📖 | |
| CHECK constraint (4d) | 📖 | |
| PRIMARY KEY constraint (4a) | 📖 | |
| DEFAULT constraint and IDENTITY (4a, 4e) | 📖 | |
| SEQUENCE (5) | ✅ | #258 — project and client reference codes (PROJ-003, CLI-0042) |
| FOREIGN KEY constraints (4b) — creating and expanding | 📖 | Already in schema |

---

## SECTION 6 — Views and Triggers
| Lecture | Issue | Notes |
|---|---|---|
| Creating a view (7) | ✅ | #259 |
| View options (WITH CHECK OPTION, SCHEMABINDING) | ✅ | #259 |
| Problems with INSERT into complex views (7) | ✅ | #259 |
| Instead-of INSERT trigger (11) | ✅ | #259 |
| Instead-of UPDATE trigger (11) | ✅ | #259 |

---

## SECTION 7 — Subqueries, CTEs, Window Functions
| Lecture | Issue | Notes |
|---|---|---|
| Derived tables and subqueries | 📖 | Prerequisite knowledge for #211 |
| Correlated subqueries (18) | ✅ | #260 |
| ROW_NUMBER, RANK, DENSE_RANK, NTILE (13) | ✅ | #211 |
| LAG and LEAD (13) | ✅ | #211 |
| FIRST_VALUE and LAST_VALUE (13) | ✅ | #211 |
| CUME_DIST, PERCENT_RANK, PERCENTILE_CONT/DISC (13) | ✅ | #211 |
| Aggregations using window functions (13) | ✅ | #211 |
| CTEs — non-recursive (12) | ✅ | #211 |
| CTEs — recursive (12) | ✅ | #260 |
| UNION, UNION ALL, EXCEPT, INTERSECT | 📖 | |

---

## SECTION 8 — Stored Procedures, Error Handling, Functions
| Lecture | Issue | Notes |
|---|---|---|
| Creating stored procedures (10) | ✅ | #210 |
| Error handling — TRY/CATCH, RAISERROR (19) | ✅ | #261 |
| Scalar functions (8) | ✅ | #261 |
| Table-valued functions (9) | ✅ | #210 |

---

## SECTION 9 — JSON
| Lecture | Issue | Notes |
|---|---|---|
| Introducing the JSON data type | ✅ | #209 |
| JSON_OBJECT, JSON_ARRAY, JSON_ARRAYAGG (14a-c) | ✅ | #212 |
| JSON_CONTAINS (14d) | ✅ | #212 |
| OPENJSON table-valued function (14e) | ✅ | #212 |
| JSON_VALUE (14f) | ✅ | #212 |
| Implementing JSON columns and indexes | ✅ | #209 |

---

## SECTION 10 — Regular Expressions
| Lecture | Issue | Notes |
|---|---|---|
| REGEXP_LIKE (15a) | ✅ | #213 |
| More about regular expressions | ✅ | #213 |
| REGEXP_REPLACE (15b) | ✅ | #213 |
| REGEXP_SUBSTR, INSTR, COUNT (15c-e) | ✅ | #213 |
| REGEXP_MATCHES and SPLIT_TO_TABLE (15f-g) | ✅ | #213 |

---

## SECTION 11 — Fuzzy String Matching
| Lecture | Issue | Notes |
|---|---|---|
| Fuzzy string matching functions (16) | ✅ | #214 |

---

## SECTION 12 — Graph Tables
| Lecture | Issue | Notes |
|---|---|---|
| Creating node and edge graph tables, MATCH (2e, 17) | ✅ | #262 |
| More about graph tables | ✅ | #262 |

---

## SECTION 13 — Indexes
| Lecture | Issue | Notes |
|---|---|---|
| Why create indexes (1d) | ✅ | #263 |
| SARGable queries and index benefits (1d) | ✅ | #263 |
| Creating indexes (1d) | ✅ | #263 |
| Developing additional indexes (1d) | ✅ | #263 |
| Partitioning (6) | ✅ | #264 |
| Expanding partitioning for tables and indexes (6) | ✅ | #264 |
| Columnstore indexes (1e) | ✅ | #265 |

---

## SECTION 14 — Specialized Tables
| Lecture | Issue | Notes |
|---|---|---|
| In-memory (memory-optimised) tables (2a) | ✅ | #266 |
| Temporal tables (2b) | ✅ | #208 |
| External tables (2c) | ✅ | #266 |
| Ledger tables (2d) | ✅ | #266 |

---

## SECTION 15 — Microsoft Fabric Setup
| Lecture | Issue | Notes |
|---|---|---|
| Signing into Microsoft Fabric | 🔵 | Fabric free trial — not local |
| Why do I need a Work email? | 🔵 | Fabric only |
| Creating a Fabric capacity and workspace | 🔵 | Fabric only |
| Creating a Fabric SQL Database | 🔵 | Fabric only |
| Installing VS Code with MS SQL Extension | 📖 | Already installed |

---

## SECTION 16 — AI-Assisted Tools
| Lecture | Issue | Notes |
|---|---|---|
| Enable GitHub Copilot in SSMS (21a) | ✅ | #215 |
| Enable GitHub Copilot in VS Code (21a) | ✅ | #215 |
| Enable Microsoft Copilot in Fabric (21b) | 🔵 | Fabric only |
| Security impact of AI-assisted tools (20) | ✅ | #215 |
| Configure model and MCP tool options (22) | ✅ | #215 |
| GitHub Copilot instruction files in VS Code (23a) | ✅ | #215 |
| MCP server endpoints + instruction files in Fabric (24, 23b) | 🔵 | Fabric only — local MCP in #215 |

---

## SECTION 17 — Security
| Lecture | Issue | Notes |
|---|---|---|
| Data encryption (25) — TDE, Always Encrypted | ✅ | #267 |
| Secure database access — Azure SQL (29a) | ✅ | #267 (links to AZ-400 #249) |
| Secure database access — Fabric (29b) | 🔵 | Fabric only |
| Object-level permissions — SQL Server Auth (28a) | ✅ | #267 |
| Further object- and column-level permissions (28a) | ✅ | #267 |
| Object-level permissions — Entra ID (28a, 28b) | ✅ | #267 |
| Dynamic Data Masking (26) | ✅ | #216 |
| Row-Level Security (27) | ✅ | #267 |
| Auditing — Azure SQL (30a) | ✅ | #268 |
| Auditing — Fabric (30b) | 🔵 | Fabric only |

---

## SECTION 18 — Performance
| Lecture | Issue | Notes |
|---|---|---|
| Recommend database configurations — installing SQL Server (33) | 📖 | Config knowledge |
| Recommend database configurations — configuring database (33) | 📖 | Config knowledge |
| Blocking transactions and locking (34, 36) | ✅ | #269 |
| Transaction isolation levels and concurrency (34, 36) | ✅ | #269 |
| Query execution plans — Part 1 & 2 (35a) | ✅ | #263 |
| DMVs (35b) | ✅ | #217 |
| Query Store (35c) | ✅ | #217 |
| Query Performance Insight (35d) | 🔧 | Azure SQL Portal only — extend #217 |

---

## SECTION 19 — SQL Database Projects and CI/CD
| Lecture | Issue | Notes |
|---|---|---|
| Create, build, validate SQL Database Projects (39) | ✅ | #218 |
| Update project and deploy changes (44) | ✅ | #218 |
| Reference/static data in source control (38) | ✅ | #270 |
| Detect schema drift (43) | ✅ | #218 |
| Installing Visual Studio Community (39) | 📖 | Tooling only |
| Unit testing strategy for SQL Database Projects (37) | ✅ | #270 |
| Source control for SQL Database Projects (40) | ✅ | #218 |
| Branching, pull requests, conflict resolution (41) | ✅ | #218 |
| Creating and expanding deployment pipeline (45) | ✅ | #218 |
| Secrets management (42) | ✅ | #218 (links to AZ-400 #247) |
| Branching policies for pipelines (45a) | ✅ | #218 |
| Triggers + approvals in pipelines (45b) | ✅ | #218 |
| Authentication tables (45c) | ✅ | #218 |
| Code Owners for pipelines (45d) | ✅ | #218 (links to AZ-400 #230) |

---

## SECTION 20 — Data API Builder
| Lecture | Issue | Notes |
|---|---|---|
| Creating DAB in VS Code (46, 49) | ✅ | #219 |
| Querying via REST endpoint (47) | ✅ | #219 |
| Querying via GraphQL / GraphiQL (47) | ✅ | #219 |
| Exposing views (49) | ✅ | #219 |
| Exposing stored procedures (49) | ✅ | #219 |
| Adding table relationships (49) | ✅ | #219 |
| Configure and secure REST and GraphQL (32, 48) | ✅ | #219 |
| DAB deployment configuration (50) | ✅ | #219 |
| DAB configuration files (46) | ✅ | #219 |

---

## SECTION 21 — Azure Monitor
| Lecture | Issue | Notes |
|---|---|---|
| Recommend Azure Monitor configurations (51) | ✅ | #271 |

---

## SECTION 22 — AI Capabilities: Models and Embeddings
| Lecture | Issue | Notes |
|---|---|---|
| Evaluate external models (53) | ✅ | #220 |
| Create and manage external models (54) | ✅ | #220 |
| Using external model in SSMS with Managed Identity (31, 54, 58) | ✅ | #220 |
| Identify columns for embeddings (56) | ✅ | #220 |
| Design and implement chunks for embeddings (57) | ✅ | #220 |
| Generate embeddings (58) | ✅ | #220 |

---

## SECTION 23 — AI Capabilities: Vector Search
| Lecture | Issue | Notes |
|---|---|---|
| Vector data type, vector indexes, size (61) | ✅ | #222 |
| Generate embeddings and vector search (55g, 58, 65) | ✅ | #222 |
| VECTOR_NORMALIZE and VECTORPROPERTY (62a, 62c) | ✅ | #222 |
| VECTOR_DISTANCE (62b) | ✅ | #222 |
| ANN, ENN, vector indexes, VECTOR_SEARCH (62d, 63, 64) | ✅ | #222 |

---

## SECTION 24 — AI Capabilities: Search Types
| Lecture | Issue | Notes |
|---|---|---|
| Choose between full-text, vector, and hybrid search (59) | ✅ | #221, #222 |
| Full-text search — FULLTEXT CATALOG and INDEX (60) | ✅ | #221 |
| Full-text search — CONTAINS and FREETEXT (60) | ✅ | #221 |
| Hybrid search and reciprocal rank fusion (66-68) | ✅ | #222 |

---

## SECTION 25 — RAG
| Lecture | Issue | Notes |
|---|---|---|
| Identify RAG use cases (69) | ✅ | #223 |
| Convert structured data to JSON (71) | ✅ | #223 |
| Creating a JSON payload / prompt (71) | ✅ | #223 |
| Send prompt via sp_invoke_external_rest_endpoint (70, 72, 73) | ✅ | #223 |

---

## SECTION 26 — Change Data Features
| Lecture | Issue | Notes |
|---|---|---|
| Change Data Capture — CDC (52b, 55e) | ✅ | #272 |
| Change Tracking — CT (52c, 55b) | ✅ | #272 |
| Change Event Streaming — CES (52a, 55f) | ✅ | #272 |
| Azure Functions with SQL trigger binding (52d, 55c) | ✅ | #273 |
| Azure Logic Apps (52e, 55d) | ✅ | #273 |
| Table Triggers (55a) | ✅ | #259 (Instead-of triggers from Section 6) |

---

## SECTION 27 — Conclusion
| Lecture | Notes |
|---|---|
| Congratulations | 📖 |

---

## Summary

| Status | Count |
|---|---|
| ✅ Covered by issues (#208–#223, #258–#273) | 32 issues covering ~100 lectures |
| 🔧 Needs new issue | 0 remaining gaps identified |
| 📖 Study only / already known | ~30 lectures |
| 🔵 Microsoft Fabric only | ~15 lectures |

## Issues created (grouped, in course order)

| # | Title |
|---|---|
| #258 | SEQUENCE objects for project and client reference numbers (section 5) |
| #259 | Views and Instead-of triggers (section 6) |
| #260 | Correlated subqueries and recursive CTEs (section 7) |
| #261 | Error handling and scalar functions (section 8) |
| #262 | Graph tables — node/edge tables with MATCH (section 12) |
| #263 | Indexes, SARGable queries, and execution plans (section 13) |
| #264 | Table partitioning on TimeEntries by year (section 13) |
| #265 | Columnstore indexes for analytical queries (section 13) |
| #266 | In-memory, external, and ledger tables (section 14) |
| #267 | Data encryption, object permissions, and RLS (section 17) |
| #268 | SQL Server auditing and Azure SQL audit logs (section 17) |
| #269 | Locking, blocking, and transaction isolation levels (section 18) |
| #270 | Unit testing and reference data in SQL Database Projects (section 19) |
| #271 | Azure Monitor metric alerts for SQL Server (section 21) |
| #272 | Change Data Capture, Change Tracking, and CES (section 26) |
| #273 | Azure Functions and Logic Apps triggered by SQL changes (section 26) |
