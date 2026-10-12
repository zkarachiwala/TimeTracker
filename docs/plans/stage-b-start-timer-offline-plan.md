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

1. ✅ Schema: `ClientRequestId` (nullable `Guid`) on `TimeEntry` + filtered unique index + EF migration
   on `TimeTrackerDataContext`. (`b9aadd4`)
2. ✅ Contracts: `ClientRequestId` on `TimeEntryCreateRequest`. (`8fb2d74`)
3. ✅ Server: `TimeEntryService.CreateTimeEntry` becomes idempotent, returns `TimeEntryResponse`.
   Update `ITimeEntryService`, `HttpTimeEntryService`, `MockTimeEntryService` to match. (`a00c410`)
4. ✅ Client: local-timer-session model — extended `PendingStopSync` into `PendingTimerSync`
   (`6d5ca68`), per decision #2 below.
5. ✅ `TimerPage`: `StartTimer` is local-first/instant via `PendingTimerSync.StartAsync`; `StopTimer`
   branches on whether the entry it's stopping has a server `Id` yet (existing Stage A flow via
   `activeEntry`) or is still local-only (`IsRunningLocally` → `StopUnsyncedAsync`, collapsing into a
   single `Create` with `End` set). (`c865ca5`)
   - **Log Block decision**: attaches a `ClientRequestId` for idempotency-safe retries, but is
     deliberately *not* persisted to local storage — `PendingTimerSync` has exactly one create-slot,
     sized for the current timer session, and Log Block is an independent, possibly-concurrent
     create. Manual retry only for now (warning snackbar, no reload-survival). Confirmed this
     doesn't foreclose adding a persisted queue for it later — same `ClientRequestId`-tagged create
     call either way, purely additive.
6. ✅ On load: reconciles both queue shapes — locally-started-unsynced (`pendingCreate`), and
   synced-stop-pending (`pendingStop`) — folded into step 5's `OnInitializedAsync` change rather than
   a separate step, since both needed the same field additions.
7. ✅ Shared "disable → spinner-after-1s" helper (`BusyState`, `e6cb431`), applied to Stop, Start,
   Sync, and each Log Block chip independently. The "orange-on-failure" half was already built in
   step 5 (the pending-save card) — this step only added the in-flight feedback on top of it. Log
   Block has no `EntryRow` failure-state indicator, per the decision above — a plain warning
   snackbar covers it until offline persistence is added.
8. ✅ Tests, all done:
   - Client-side unit tests for the new sync-state logic (`PendingTimerSyncTests`) and the busy/
     spinner state machine (`BusyStateTests`) — done in steps 4 and 7 respectively.
   - Playwright regression coverage for Start-while-offline
     (`StartTimer_WhenCreateFailsOnce_ShowsRunningCardImmediatelyAndTicksAndSyncsOnStop`, `98c073b`),
     mirroring the existing `StopTimer_WhenSaveFails_...`. Verified 8/8 passing locally.
   - Container test for the `ClientRequestId` filtered unique index under a real SQL Server engine
     (`ClientRequestIdUniqueIndexTests`, `8f2e724`) — unique-violation on a repeated tag, both
     succeed on distinct tags, both succeed on a null tag (the actual SQL Server gotcha). Verified
     12/12 Category=Container tests passing locally.
   - Log-Block-while-offline coverage intentionally dropped — Log Block is manual-retry-only (no
     local persistence), so there's no reload-survival behavior to assert beyond the snackbar.

## Bugs found during manual verification (2026-09-03)

Two real bugs surfaced only by clicking through the offline scenarios by hand — bUnit can't reach
this code (`OnInitializedAsync`'s `OperatingSystem.IsBrowser()` guard is always false there), so
Playwright/manual testing was the only layer that could have caught them. Both fixed and now
covered by the new Playwright test above:
- Ticker never started for a local-only running timer, because the start-ticker check sat inside
  the same `try` as the network refresh calls and never ran when those calls threw (`169def1`).
- `StartTimer` always called `Reload()` (a full network refresh) even when the create failed to
  sync, so the ticker sat frozen for however long those doomed requests took to fail (`ec1042f`).
A third, not-yet-manually-reachable staleness bug (`SyncPending` trusting `RetryIfAnyAsync`'s
combined bool) was caught by code review and fixed alongside them (`c07dd52`).
