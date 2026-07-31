# TimeTracker — action list

Handover from the RLS / issue #167 session. Nothing is merged; no PRs are open.

## State

| Branch | Contents | Verified |
|---|---|---|
| `claude/rls-bypass-role` | 2 migrations, 4 RLS tests, docs, SDK pin fix | Builds, 173 fast tests pass. **Migrations never applied, container tests never run.** |
| `claude/rls-predicate-spike` | 7 throwaway spike tests, RLS doc, SDK pin fix | Compiles only. **Never run.** |
| `claude/issue-167-planning-xp17cc` | Budget feature | Fast tests pass. Held — bar measures your hours, not the project's. |

Issues filed: [#322](https://github.com/zkarachiwala/TimeTracker/issues/322) · [#323](https://github.com/zkarachiwala/TimeTracker/issues/323) · [#324](https://github.com/zkarachiwala/TimeTracker/issues/324) · [#325](https://github.com/zkarachiwala/TimeTracker/issues/325)

---

## 1. Verify locally — do this first

Nothing RLS-related has ever executed. Docker was unavailable in the session.

```bash
git checkout claude/rls-bypass-role
dotnet test TimeTracker.Tests --filter "Category=Container"
```

- [ ] `MigrationSmokeTests` pass — both migrations apply cleanly from scratch
- [ ] `RlsIntegrationTests` pass — 7 tests, 4 of them new

**If the new tests fail**, the first thing to suspect is `IS_MEMBER('rls_bypass')` inside the `SCHEMABINDING` predicate. `IS_ROLEMEMBER('rls_bypass')` is the alternative. `IS_MEMBER` was chosen because the existing migration proves it works in that context, but it was never tested with a custom role.

Then run the spike, which answers the open design questions for project-level budgets:

```bash
git checkout claude/rls-predicate-spike
dotnet test TimeTracker.Tests --filter "FullyQualifiedName~RlsPredicateSpike" \
  --logger "console;verbosity=normal"
```

- [ ] Read the `[Q1]`…`[Q7]` lines in the console output — the findings are the deliverable, not pass/fail

---

## 2. Project board

`gh` was unavailable and the GitHub MCP tools have no project-board operation, so this is manual.

```bash
for n in 322 323 324 325; do
  gh project item-add 1 --owner zkarachiwala \
    --url https://github.com/zkarachiwala/TimeTracker/issues/$n
done
```

- [ ] Add all four to project #1
- [ ] Set Priority: 🔴 #322 · 🟡 #323 · 🟡 #324 · 🟢 #325

---

## 3. Azure — before migration 2 reaches production

**Read this before deploying.** `RemoveDbOwnerRlsExemption` drops the `db_owner` escape hatch. If the backup principal is not in `rls_bypass` by then, the nightly `.bacpac` exports **empty tables and does not error** — a `FILTER` predicate hides rows rather than failing.

The role does not exist until migration 1 runs, so either create it manually first (recommended — one deploy) or split the deploys.

### Recommended: create the role in Azure first

Connect to `timetracker-sql.database.windows.net` / `TimeTrackerDb` with your Azure AD admin account:

```sql
CREATE ROLE rls_bypass;

ALTER ROLE rls_bypass    ADD MEMBER [timetracker-github-backup];
ALTER ROLE db_ddladmin   ADD MEMBER [timetracker-github-backup];
ALTER ROLE db_datareader ADD MEMBER [timetracker-github-backup];
```

- [ ] Role created and memberships granted
- [ ] Wait for one nightly backup, or trigger `backup.yml` manually
- [ ] **Check the `.bacpac` in `TimeTracker-backups` is a plausible size** — an empty-table export is the failure mode, and it looks like a normal successful run
- [ ] Only then drop the old role:
  ```sql
  ALTER ROLE db_owner DROP MEMBER [timetracker-github-backup];
  ```
- [ ] Deploy both migrations together — migration 1's `CREATE ROLE` is guarded by `IF NOT EXISTS`, so it is a no-op

### Alternative: split the deploys

Merge migration 1 only → run the `ALTER ROLE` statements → verify a backup → merge migration 2.

---

## 4. Expect these changes locally

After migration 2, both are correct behaviour, not regressions:

- **`sa` returns nothing** from `app.TimeEntries` in SSMS. To inspect data:
  ```sql
  ALTER ROLE rls_bypass ADD MEMBER [sa];
  -- ... investigate ...
  ALTER ROLE rls_bypass DROP MEMBER [sa];
  ```
- **`GetProjectUsers` returns only yourself.** This bug already exists in production; it just becomes reproducible locally for the first time.

---

## 5. Decisions still open

- [ ] **`claude/rls-predicate-spike`** — merge it, or keep it local and delete after reading the output? It is throwaway by design. The one test worth keeping is `Q7_AggregateUnderRls_SilentlyReturnsPartialSum`, which belongs in `RlsIntegrationTests`.
- [ ] **The SDK pin fix** (`global.json`, dropping `rollForward: disable`) is on both branches. They merge cleanly in either order, but it is duplicated.
- [ ] **#167** is held pending project-level budget rollup. That needs `ProjectUser.Role`, a widened RLS predicate, **and** removing the `.Where(te => te.UserId == userId)` filter in `TimeTracker.Web/Features/TimeEntries/TimeEntryService.cs:26`. The `rls_bypass` role does **not** fix it on its own.

---

## 6. Correction worth knowing

`db_owner` is **not** exempt from RLS by SQL Server design — Microsoft's docs say a `dbo` user, `db_owner` member or table owner is filtered like anyone else. This project's `sa` bypass came entirely from the `OR IS_MEMBER('db_owner') = 1` clause added by `ExemptDbOwnerFromRls`.

The claim had spread to `decisions.md` (twice) and `azure-deployment.md`; all are now corrected on `claude/rls-bypass-role`. `architecture.md:102` already had it right.

Practical consequence: **any new predicate must include an exemption clause deliberately**, or the backup silently breaks.

Full write-up: `docs/rls-security-model.md`.
