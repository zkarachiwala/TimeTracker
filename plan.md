# TimeTracker — Active Plan

## Pending tasks (pre-Phase 4)

### 1. Rename TimeTracker.API → TimeTracker.Web ✅ (PR #26)

Align the project name with the documentation (roadmap.md refers to `TimeTracker.Web`).

**Changes required:**
- Rename directory `TimeTracker.Web/` → `TimeTracker.Web/`
- Rename `TimeTracker.Web.csproj` → `TimeTracker.Web.csproj`
- Update root namespace and assembly name in the csproj
- Update all `using TimeTracker.Web.*` references throughout the project to `using TimeTracker.Web.*`
- Update `TimeTracker.sln` project reference
- Update `TimeTracker.Tests/TimeTracker.Tests.csproj` project reference
- Update `CLAUDE.md` commands that reference `TimeTracker.Web`
- Update `README.md` references

**Branch:** `feature/rename-api-to-web`

---

### 2. Replace PNG ERDs with Mermaid diagram in architecture.md ✅ (PR #26)

Remove the two static PNG files from the repo root and replace with a live Mermaid ERD embedded directly in `docs/architecture.md`.

**Files to remove:**
- `master - TimeTrackerDb - app.png`
- `master - TimeTrackerDb - id.png`

**Mermaid diagram should cover:**
- `app` schema: `TimeEntries`, `Projects`, `ProjectDetails`, `ProjectUsers`
- `id` schema: ASP.NET Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.)
- Relationships between entities

**Branch:** can be done on the same `feature/rename-api-to-web` branch or separately

---

## Phase 4 (next after above tasks)

**Google OAuth + cookie auth** — branch `feature/google-auth`

See `docs/roadmap.md` for full spec.
