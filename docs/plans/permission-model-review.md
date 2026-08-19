# Permission model review

> **Status: discussion document. No decision made.**
>
> The defects found during this review are tracked as issues (#337–#341) and are actionable
> independently. The architectural question in Part 3 is **open** — nothing here commits to it.

---

## Part 1 — The placement principle

Three tests decide whether a rule belongs in Row-Level Security rather than the application:

1. **Is it invariant?** Does it ever have a legitimate exception?
2. **Is breach catastrophic?** Or merely embarrassing?
3. **Is it join-free?** Or does the predicate need `EXISTS (SELECT … JOIN …)` evaluated per row?

Tenant isolation passes all three. *"Your rows only"* passes only in an application where users
genuinely never share — which this one is not, because team rollups are wanted. Per-project
visibility fails all three.

This is the root of the current strain: **a variable business rule is encoded as an invariant.**
Rules like that do not fail loudly. They under-report.

### Where each rule belongs

| Layer | Enforces | Test |
|---|---|---|
| RLS | Tenant boundary, nothing else | Invariant + catastrophic + join-free |
| EF global query filters | Unconditional truths (soft delete) | Never has an exception |
| Policy + claim | Coarse capability ("may manage members") | Fixed for the session |
| Resource handler | Per-resource permission ("may manage *this* project") | Needs the resource in hand |
| Service `WHERE` | Ownership ("is this record mine") | It is a column, not a role |

---

## Part 2 — What the current model costs

| Symptom | Cause |
|---|---|
| #167 budget bar shows one person's hours as if they were the project's | `SUM` over RLS-filtered rows returns a plausible partial total, no error |
| `GetProjectUsers` returns only yourself | `ProjectUsersUserPolicy` |
| Admin Projects and Trash pages omit unassigned projects **in production** | `ProjectsUserPolicy` (#337) |

`docs/rls-security-model.md` already documented the ceiling — *"Anything team-wide is blocked by
design"* — with both casualties listed. This review acts on a diagnosis that was already made.

### Tracked defects

| Issue | Defect |
|---|---|
| #337 | `ProjectsUserPolicy` hides unassigned projects from admin pages in production |
| #338 | `ProjectUser` time-allocation gate not enforced on writes |
| #339 | Role change does not rotate the security stamp — 30-minute admin retention |
| #340 | Session context leaks across pooled connections when there is no user |
| #341 | Endpoint authorization corrections (`/api/dev/login`, `revoke-sessions`, project users) |

None of these depend on the architectural question. They are true in any direction.

### What is sound

The **machinery** is correct and transferable: the per-command interceptor (correctly handling
connection pooling), `rls_bypass` as an explicit revocable grant rather than an implicit `db_owner`
exemption, the Testcontainers harness with two differently-privileged logins, the audit trail.

What is misplaced is the predicate — the `WHERE` clause, not the mechanism.

---

## Part 3 — The open question: does a tenant tier happen?

**Undecided. Recorded here so the reasoning is not lost, not because it is agreed.**

ADR-023 declares multi-tenancy explicitly out of scope, and made that call well on product grounds.
Adopting a tenant tier would be **changing the goal** — from "cheapest correct personal app" to
"reference implementation worth pointing at" — not correcting an error. If it happens, ADR-023
should be superseded explicitly, recording that learning value was the deciding factor.

### If it happens, the shape

```
Tenant(Id, Name, Slug)
TenantMembership(TenantId, UserId, TenantRole)              -- Owner | Admin | Member
Project(Id, TenantId, Name)
ProjectMembership(ProjectId, TenantId, UserId, ProjectRole) -- Manager | Member | Viewer
WorkRecord(Id, TenantId, ProjectId, UserId, …)
```

One predicate function, `FILTER` + `BLOCK`, applied to every tenant-scoped table. Per-user
visibility moves to the application tier, which is what unblocks team rollups (#167).

**Free win:** `Clients` gets RLS for the first time. ADR-020 deferred it because it needed a two-hop
predicate; with a direct `TenantId` the same one-line predicate covers it.

### RBAC notes

`AspNetUserRoles` is `(UserId, RoleId)` — there is nowhere to put a tenant. Roles stored there are
global to the user, live in a different schema to tenant membership with no FK between them, and
cannot be protected by RLS. `UserManagementService`'s last-admin guard (`GetUsersInRoleAsync`)
would count admins across all tenants.

Role authority would need to move into the application schema. Identity keeps doing authentication;
it stops being the authorization store.

Two role axes, and one of them cannot be a claim: a user might manage 3 of 50 projects, and
per-project claims exceed the ~4KB cookie ceiling. That constraint *forces* resource-based
authorization rather than it being a stylistic choice.

### Honest cost

Multi-tenancy is permanent complexity, not just build cost. Every new table needs a `TenantId` and a
policy. Every migration needs bypass discipline. Every aggregate needs checking. On an application
with one real user, that is carried forever for a tenant count of one.

### Prerequisites either way

- **#323** must be settled first. The container suite is the only automated proof the policies
  behave, and until it has actually run against the engine used for development, it proves less
  than it appears to. PR #336 makes the versions agree but does not verify them.
- The spike referenced in `docs/rls-security-model.md` as existing (`RlsPredicateSpike` in
  `TimeTracker.Tests/Infrastructure/`) **does not exist**. The open questions it was meant to answer
  — whether `BLOCK` predicates behave as expected against EF-generated `UPDATE`s, whether the
  interceptor's separate `sp_set_session_context` command shares the session under a `BLOCK`
  predicate — are unanswered.
- Any backfill is the highest-risk step. A `FILTER` predicate applies to `UPDATE`, so an unbypassed
  backfill touches zero rows, reports success, and raises nothing. It must run under `rls_bypass`
  and assert its own completeness rather than trusting the row count.

---

## Part 4 — Test strategy

RLS cannot be unit tested at any provider — it is a SQL Server engine feature. EF Core InMemory has
no policies and no `SESSION_CONTEXT`, so the entire RLS layer is invisible to
`--filter "Category!=Container"`.

Microsoft recommend against EF Core InMemory generally: it is not relational, does not enforce
foreign keys or unique constraints, and lets tests pass that would fail against SQL Server.

If the suite is reworked, the principle worth holding is:

> **No test uses a fake database. Tests either use no database at all, or a real one.**

| Tier | What lives there | Docker? |
|---|---|---|
| No DB | `AwardRateResolver`, Mapster config, endpoint policy wiring, middleware, and — if built — permission maps and `AuthorizationHandler`s | No |
| Real SQL Server | `TimeEntryService`, `ProjectService`, `ClientService`, `ProjectUser`, Identity-backed tests, RLS, migration smoke | Yes |

Roughly half the current files need no database at all. Running an `AuthorizationHandler` test
against SQL Server buys nothing — it is a pure class.

Two gotchas for that migration:

- **Respawn must connect as `rls_bypass`.** Reset works by `DELETE`, and a `FILTER` predicate
  applies to `DELETE` — a reset connection without the bypass deletes only its own rows, reports
  success, and leaves the table dirty. `timetracker_rls_bypass_test` already exists for this.
- **`AuthEndpointAuthTests` uses InMemory only to satisfy Identity DI**, not to test data. It
  belongs in the no-DB tier.

Note the cost: dropping InMemory entirely means every test requires Docker, and remote sessions
(Claude Code on the web) lose the ability to run any test.

### Properties, not percentages

Coverage measures which lines ran, not which guarantees hold. For a permission system the useful
question is which properties are proven, and by which suite:

| Property | Suite |
|---|---|
| Cross-tenant read returns nothing | Container |
| Cross-tenant write throws (`BLOCK`) | Container |
| Unset session context returns nothing — fails closed | Container |
| Non-member cannot log time to a project | Unit |
| Non-manager cannot see others' entries | Unit |
| A user can always edit their own record | Unit |
| Demoted admin loses access | Integration |
| Every endpoint carries its intended policy | Metadata |

There is currently no coverage measurement at all — that is #234.

---

## Related

- `docs/rls-security-model.md` — how the current policies work, and the gaps it already records
- `docs/decisions.md` — ADR-020 (RLS), ADR-023 (single-tenant), ADR-024 (`ProjectUser` as gate),
  ADR-031 (pipeline migrations), ADR-033 (`rls_bypass` role)
- Issues #337–#341 (defects), #323 (engine parity), #167 (blocked by current design), #234 (coverage)
