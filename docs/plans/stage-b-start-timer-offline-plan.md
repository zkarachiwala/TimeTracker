# Stage B — start a timer offline

Detailed implementation plan for Stage B of `offline-resilience-plan.md`. Stage A (auth-state fix,
pending-stop sync, `PendingStopSync`) is merged (PR #390). This plan grounds Stage B in the current
code and exists to be discussed and revised before any code is written.

## What Stage B adds

Today, `TimerPage.StartTimer` and `LogBlock` both call `ITimeEntryService.CreateTimeEntry` directly
and block on a server round trip before anything shows up. If the backend is cold/unreachable, Start
fails outright — there is no local existence for a timer until Stage B.

Stage B makes Start (and logging a fixed block) instant and local, syncing to the server in the
background, with the same "don't lose or duplicate data" guarantee Stage A already gives Stop.

## Current code this touches

- `TimeTracker.Shared/Entities/TimeEntry.cs` — no `ClientRequestId` yet.
- `TimeTracker.Contracts/Features/TimeEntries/TimeEntryModels.cs` — `TimeEntryCreateRequest` has no
  `ClientRequestId`.
- `TimeTracker.Web/Features/TimeEntries/TimeEntryService.cs:70` — `CreateTimeEntry` returns `Task`
  (void), always inserts.
- `TimeTracker.Web/Data/TimeTrackerDataContext.cs:16` — `OnModelCreating`; existing precedent for
  `HasIndex(...).IsUnique()` (see `Client.Name`).
- `TimeTracker.Client/Features/Timer/PendingStopSync.cs` — Stage A's queue, but it only knows how to
  model "an entry that already has a server `Id`, needs its `End` synced." Stage B's local-started
  timer has no server `Id` yet, which is a different shape.
- `TimerPage.razor` — `StartTimer()` (line ~287) and `LogBlock()` both call `CreateTimeEntry`
  directly and block on it.

## Design decisions to confirm before coding

1. **`CreateTimeEntry`'s signature changes** from `Task` to `Task<TimeEntryResponse>` (idempotent —
   returns the existing row if `ClientRequestId` is already present, instead of inserting a
   duplicate). This is a breaking interface change touching `ITimeEntryService`, `TimeEntryService`
   (server), `HttpTimeEntryService` (client), `MockTimeEntryService` (showcase), and every existing
   caller (`StartTimer`, `LogBlock`, and any test doubles).

2. **Extend `PendingStopSync` into a wider `PendingTimerSync`, or add a new class alongside it?**
   Leaning toward extending/renaming — Stage A's class already owns "the local timer's pending sync
   state" conceptually; Stage B widens that state machine rather than being a separate concern. The
   states become: *started locally, not yet synced* (has `ClientRequestId`, no server `Id`) →
   *synced, running* (has server `Id`, matches today's `activeEntry`) → *synced, stop pending*
   (today's `PendingStopSync.PendingStop`, unchanged).

3. **Collapsing start+stop while still offline.** If Start never made it to the server before Stop is
   clicked, the queued mutation should become a single `Create` carrying both `Start` and `End` —
   never a `Create` followed by an `Update`, which would need a local→server ID mapping (the fiddliest
   part of any sync engine, per the original plan doc). Only once a local timer has synced and has a
   real server `Id` does a later offline Stop fall back to Stage A's existing `Update`-based flow.

4. **SQL Server unique-index gotcha**: a plain unique index in SQL Server only tolerates a *single*
   `NULL` (unlike Postgres, which tolerates many) — most `TimeEntry` rows will have `ClientRequestId
   = NULL`. Must use a **filtered** unique index (`HasFilter("[ClientRequestId] IS NOT NULL")`), not
   a plain `HasIndex(...).IsUnique()`, or the second-ever synced-online entry would fail to insert.

5. **Sync timing**: Stage A retries a pending stop on page load only. Stage B's premise — "started
   while offline, possibly for a while" — may span more than one page load in the same session;
   worth deciding whether to also retry periodically while the page stays open, not just on load.

## Decisions (resolved 2026-09-02)

- **`PendingStopSync` → `PendingTimerSync`**: extend it, don't add a parallel class.
- **`CreateTimeEntry` signature change**: confirmed acceptable. Blast radius analysed and bounded —
  6 files change purely mechanically (`ITimeEntryService`, `TimeEntryService`, `HttpTimeEntryService`,
  `MockTimeEntryService`, `TimeEntryEndpoints`, `FakeServices.cs`); `TimerPage`'s two call sites
  change anyway as the actual feature work; `TimeEntryServiceTests.cs` and `EntrySheet.razor` need no
  forced change.
- **Collapse start+stop while offline**: confirmed, applies to Stop *and* Log Block — both are a
  single `Create` with `End` already set when they were never online for the start.
- **Future offline-editing**: not built now, but the schema and pattern already accommodate it without
  rework later (`ClientRequestId` is a permanent per-row identity, not just a create-dedup token).
  Deliberately **not** shaping the queue for multi-mutation/ordered edits now — that's real new
  correctness work (partial-failure semantics, merge/ordering logic) worth its own discussion when
  offline editing is actually greenlit, not spec'd speculatively here.

## UX: avoiding a false "problem" flash on a healthy save (resolved 2026-09-02)

Found during Stage A manual testing: the pending-sync (orange "Sync") card was appearing briefly even
on a fast, successful save — because the UI switches to it optimistically the instant Stop is clicked,
before the network result is known. Researched against actual evidence rather than guessing
([NN/g — Response Time Limits](https://www.nngroup.com/articles/response-times-3-important-limits/),
[NN/g — Mask Interaction Delays with Progress Indicators](https://www.nngroup.com/videos/progress-indicators/)):

- **0.1s–1.0s**: no special feedback is warranted beyond disabling the control — this is where a
  healthy save typically lands, so no "Syncing" state is justified for the common case.
- **Past ~1s**: this is where a progress indicator earns its keep (demonstrably reduces perceived
  wait) — and this is the actually-relevant case here (a cold/retrying backend).

**Resolved pattern, applies uniformly to Stop, Start, and Log Block:**
1. Disable the control immediately on click — always, prevents double-submit, no color/label change.
2. If the save is still unresolved after ~1s, *then* show a spinner (neutral color, not orange —
   outcome still unknown).
3. On genuine failure, the existing orange "not yet saved / Sync" treatment.

Implementation note: this is one small reusable piece (race the actual save `Task` against
`Task.Delay(1000)`, flip to spinner state only if the delay wins and the save hasn't completed) — build
it once, use it for all three actions. Log Block has no persistent card to show a failure state on
(it's a one-off chip click that creates a historical entry, not an ongoing timer) — a failed Log Block
needs a pending-sync indicator on that entry's row in Today's list once it appears there. That's new UI
surface (`EntryRow` has no pending-state concept today) — its exact design is deferred to when Log
Block's offline piece is actually implemented, not decided in the abstract now.

## Task breakdown (sequenced)

1. Schema: `ClientRequestId` (nullable `Guid`) on `TimeEntry` + filtered unique index + EF migration
   on `TimeTrackerDataContext`.
2. Contracts: `ClientRequestId` on `TimeEntryCreateRequest`.
3. Server: `TimeEntryService.CreateTimeEntry` becomes idempotent, returns `TimeEntryResponse`.
   Update `ITimeEntryService`, `HttpTimeEntryService`, `MockTimeEntryService` to match.
4. Client: local-timer-session model (extend `PendingStopSync` or new class per decision above).
5. `TimerPage`: `StartTimer` becomes local-first/instant; `StopTimer` branches on whether the entry
   it's stopping has a server `Id` yet (existing Stage A flow) or is still local-only (collapse into
   a single `Create` with `End` set).
6. On load: reconcile both queue shapes — locally-started-unsynced, and synced-stop-pending.
7. Shared "disable → spinner-after-1s → orange-on-failure" helper, applied to Stop, Start, and Log
   Block uniformly (see UX section above). Log Block's failure-state indicator on `EntryRow` is its
   own small design task within this step, not pre-specified.
8. Tests: EF migration/idempotency container test (unique index under RLS, per the original plan's
   own Verification section), client-side unit tests for the new sync-state logic (plain class +
   fakes, no bUnit — same approach as `PendingStopSyncTests`), Playwright regression coverage
   mirroring `TimerTests.StopTimer_WhenSaveFails_...`, plus new coverage for Start-while-offline and
   Log-Block-while-offline.
