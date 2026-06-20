# Issue #138 — Calendar View: Monthly Time Entry Overview

## Library Recommendation: `Heron.MudTotalCalendar`

**NuGet:** `Heron.MudTotalCalendar` (v4.0.0, MIT license, 12k+ downloads)

### Why this library

- **Purpose-built**: "A calendar component for displaying numerical data with totals" — exactly matches the requirement of showing daily hour totals in a monthly grid.
- **MudBlazor native**: Built on top of `Heron.MudCalendar`, which is a MudBlazor calendar component (321 stars, 488k downloads). Inherits MudBlazor theming, dark mode, responsive layout.
- **Free tier**: MIT license. No paid tiers.
- **.NET 8+ compatible**: v4.x works with .NET 8/9/10.
- **Key features for this issue**:
  - Monthly calendar grid (also supports week, work week, day views)
  - Each day cell can display multiple numeric values with different colors/styles
  - Built-in day/week/month totals with `ShowDayTotal`, `ShowWeekTotal`, `ShowMonthTotal`
  - `CellTemplate` for fully custom day cell rendering (e.g., show project breakdowns)
  - `TotalTemplate` for custom total display formatting
  - Built-in month navigation (prev/next)
  - Click handlers on cells available via `CalendarItem` events
- **Dependencies**: `Heron.MudCalendar` (also MIT) + `MudBlazor` (already in project)

### Why not alternatives

| Library | Reason rejected |
|---------|----------------|
| Syncfusion.Blazor.Schedule | Commercial license (community license has usage limits) |
| BlazorCalendar (tossnet) | Fewer features, no total-calendar mode, less actively maintained |
| BlazorFullCalendar | JS interop wrapper, not MudBlazor-native |
| Smart.Blazor | Large component suite, not free |

---

## Design Decision: Supplement, Not Replace

The calendar view will be a **5th filter tab** alongside Day/Month/Year/Project. The existing list view is useful for detailed scrolling and editing — the calendar adds an at-a-glance overview. Users choose the best mode for their current task.

### Benefits of this approach
- No new route needed (stays on `/entries`)
- Shares all data loading, error handling, FAB, and EntrySheet from existing page
- Users can switch between calendar and list views instantly
- Zero breaking changes to existing UX

---

## Data Flow

```
TimeEntriesPage.razor
    │
    ├── LoadEntries()
    │       └── TimeEntryService.GetAllTimeEntriesByYear(cursor.Year)
    │               └── GET /api/timeentries/year/{year}/all
    │                       → List<TimeEntryResponse> (all entries for year)
    │
    ├── Calendar tab (new tab 4)
    │       │
    │       ├── Transform: entries → List<CalendarItem> (one per entry, Start + End + Text)
    │       │
    │       ├── Transform: entries → List<Value> (aggregated daily totals per project)
    │       │       │
    │       │       └── ValueDefinition per project (Name = project name, color from ProjectColors)
    │       │           Value.Amount = total seconds for that project on that day
    │       │
    │       └── <CalendarView Items="..." Values="..." OnDaySelected="HandleDayClick" />
    │
    └── Day click → sets cursor.Date → switches to Day tab showing that day's list
```

### API consideration
`GetAllTimeEntriesByYear` already fetches all entries for the year — no new endpoint needed. The calendar is a pure client-side view transform of existing data. This avoids the "over-fetching" concern mentioned in the issue (we're already fetching the year's data for the list view).

---

## Future Extensibility: Multi-User Management Calendar

The same `Heron.MudTotalCalendar` component naturally supports a future management view where multiple users' time appears color-coded on the same grid.

### How it maps to the library's data model

The library's core abstraction is:

```
ValueDefinition (name, color, format)   ← 1 per "what's being tracked"
    └── Value (date, definition, amount) ← 1 per day per definition
```

| Today (Phase 10) | Future management view |
|-------------------|----------------------|
| `ValueDefinition` per **project** | `ValueDefinition` per **user** (or per user×project) |
| `Value.Amount` = total seconds on project that day | `Value.Amount` = total seconds by that user that day |
| One `CalendarItem` per time entry | One `CalendarItem` per time entry, tagged with user |
| `CellTemplate` shows project breakdown | `CellTemplate` shows user breakdown with avatar/color |

### Design for extensibility today

**1. Extract the transform logic into its own class** — not inline in the component:

```
TimeTracker.Client/Features/TimeEntries/Services/CalendarDataTransformer.cs
├── ToCalendarItems(List<TimeEntryResponse>) → List<CalendarItem>
├── ToDailyValues(List<TimeEntryResponse>) → List<Value>    // per-project grouping
└── (future) BuildValues(List<TimeEntryResponse>, GroupingMode) → List<Value>
```

Today `GroupingMode` is always "by project". The interface stays the same — `CalendarView` just receives `List<Value>` regardless of how they were grouped. In the future, a management page can call `BuildValues(entries, GroupingMode.ByUser)` with a different definition set.

**2. CalendarView component is grouping-agnostic:**

```csharp
[Parameter] public List<CalendarItem> Items { get; set; } = [];   // event bars
[Parameter] public List<Value> Totals { get; set; } = [];          // colored daily sums
[Parameter] public EventCallback<DateTime> OnDaySelected { get; set; }
```

The component doesn't know or care whether values represent projects, users, or both. It just renders whatever colored value bars it's given.

**3. Future backend API changes (out of scope for #138):**

A management calendar would need:
- `GET /api/timeentries/admin/month/{month}/year/{year}` — fetches entries for all users (Admin-only)
- Each `TimeEntryResponse` would need a `UserName` field (or a separate `AdminTimeEntryResponse` DTO)
- A new `AdminCalendarPage` at `/admin/calendar` (Admin-only, not in bottom nav for regular users)

**4. Visual example of the future state:**

```
┌──────────────────────────────────────┐
│  June 2026                           │
├────┬────┬────┬────┬────┬────┬────┐
│    │    │    │ 1  │ 2  │ 3  │ 4  │
│    │    │    │6h15 │4h30 │7h00 │3h20 │
│    │    │    │━━━━ │━━━━ │━━━━ │━━━━ │
│    │    │    │🟢AK │🟢AK │🔵ZB │🟢AK │
│    │    │    │🔵ZB │🔴CM │     │🔴CM │
├────┼────┼────┼────┼────┼────┼────┤
│ 5  │ 6  │ 7  │ 8  │ ...              │
│5h45 │6h00 │8h15 │4h50 │               │
│━━━━ │━━━━ │━━━━ │━━━━ │               │
│🟡DJ │🟢AK │🟡DJ │🔵ZB │               │
│🟢AK │🔵ZB │     │     │               │
└────┴────┴────┴────┴───────────────────┘

Legend: 🟢 Alice K  🔵 Zak B  🔴 Carlos M  🟡 Dave J
```

Each colored bar = one user, height/opacity = hours worked. Same `ValueDefinition.Style` mechanism, just different grouping key.

---

## Implementation Plan

### Step 1: Add NuGet packages

**File: `TimeTracker.Client/TimeTracker.Client.csproj`**
```xml
<PackageReference Include="Heron.MudCalendar" Version="4.0.0" />
<PackageReference Include="Heron.MudTotalCalendar" Version="4.0.0" />
```

### Step 2: Add @using statements

**File: `TimeTracker.Client/_Imports.razor`**
```razor
@using Heron.MudCalendar
@using Heron.MudTotalCalendar
```

### Step 3: Add CSS/JS references (if needed)

The library auto-injects its references. If display issues occur, add to `TimeTracker.Web/App.razor`:
```html
<link href="_content/Heron.MudCalendar/Heron.MudCalendar.min.css" rel="stylesheet" />
<link href="_content/Heron.MudTotalCalendar/Heron.MudTotalCalendar.min.css" rel="stylesheet" />
<script type="module" src="_content/Heron.MudCalendar/Heron.MudCalendar.min.js"></script>
```

### Step 4: Create CalendarDataTransformer service

**New file: `TimeTracker.Client/Features/TimeEntries/Services/CalendarDataTransformer.cs`**

Extracted from the component to keep it grouping-agnostic (see Future Extensibility above). A static/stateless class with two methods:

```csharp
public static class CalendarDataTransformer
{
    // One CalendarItem per time entry (for event bars in the calendar)
    public static List<CalendarItem> ToCalendarItems(List<TimeEntryResponse> entries) { ... }

    // Daily totals grouped by project → List<Value> for MudTotalCalendar
    public static List<Value> ToDailyProjectValues(List<TimeEntryResponse> entries) { ... }
}
```

`ToDailyProjectValues` groups entries by `Start.Date` then by `Project.Id`, creates one `ValueDefinition` per project (colored via existing `ProjectColors.ForProject()`), and returns `Value` objects with `Amount = totalSeconds` for each day/project combination.

### Step 5: Create CalendarView component

**New file: `TimeTracker.Client/Features/TimeEntries/Components/CalendarView.razor`**

A grouping-agnostic component that receives pre-transformed data:

```csharp
[Parameter] public List<CalendarItem> Items { get; set; } = [];   // optional event bars
[Parameter] public List<Value> Totals { get; set; } = [];          // colored daily sums
[Parameter] public EventCallback<DateTime> OnDaySelected { get; set; }
[Parameter] public EventCallback<DateTime> OnMonthChanged { get; set; }
```

The component renders `<MudTotalCalendar>` with project-colored totals per day. It doesn't know or care whether values represent projects, users, or both — just renders whatever it's given.

**Rendering:**
- If `Items.Count > 0`: show time entry bars on the calendar
- `Totals`: colored stacked bars per day (one per project, color from `ValueDefinition.Style`)
- `ShowDayTotal="true"` + `ShowWeekTotal="true"`: built-in totals in the margin
- `CellTemplate` (optional, for Phase 10): custom day cell showing total hours + project breakdown
- Day click → `OnDaySelected.InvokeAsync(date)`
- Month navigation → `OnMonthChanged.InvokeAsync(newMonth)`

### Step 6: Add calendar tab to TimeEntriesPage

**File: `TimeTracker.Client/Features/TimeEntries/Pages/TimeEntriesPage.razor`**

Changes:
1. Add "Calendar" as 5th tab (index 4)
2. Store a `selectedCalendarDate` field
3. When `activeTab == 4`, render `<CalendarView>` instead of the list
4. `OnDaySelected` handler: set `cursor = date`, switch to `activeTab = 0` (Day tab) to show that day's entries
5. `OnMonthChanged` handler: update `cursor` to the new month (for year boundary handling when navigating across years — may need to reload data)

```csharp
// Tab 4: Calendar
4 => {
    // calendar fills the area, no additional filtering needed
}
```

### Step 7: Handle year boundary navigation

When the user navigates to a different year via the calendar's month stepper, we need to reload entries for that year. The `OnMonthChanged` callback handles this:
```csharp
private async Task HandleMonthChanged(DateTime newMonth)
{
    if (newMonth.Year != cursor.Year)
    {
        cursor = newMonth;
        await LoadEntries();
    }
}
```

### Step 8: Update mock service (Showcase mode)

**File: `TimeTracker.Client/Mock/MockTimeEntryService.cs`**

Ensure `GetAllTimeEntriesByYear` returns entries spread across multiple months/days so the calendar demonstrates well. Current mock data should already be sufficient (entries scattered across dates).

### Step 9: Tests

**File: `TimeTracker.Tests/Features/TimeEntries/TimeEntryServiceTests.cs`**

No API changes needed, but verify existing tests pass. Optionally add a test verifying `GetAllTimeEntriesByYear` returns entries that can be grouped by day/month for calendar transform (though this is a client-side concern, not a service test).

**File: `TimeTracker.Playwright/Tests/TimeEntriesTests.cs`**

Add a test for the calendar view:
```csharp
[Test]
public async Task CalendarView_ShowsMonthlyGrid_WithDailyTotals()
{
    await Page.GotoAsync("/entries");
    // Click Calendar tab
    await Page.ClickAsync("button:has-text('Calendar')");
    // Verify calendar is rendered
    await Expect(Page.Locator(".mud-calendar")).ToBeVisibleAsync();
    // Verify day cells show time totals
    await Expect(Page.Locator(".mud-calendar-cell")).Not.ToHaveCountAsync(0);
}
```

If write tests are enabled:
```csharp
[Test]
public async Task CalendarView_ClickDay_ShowsEntriesForThatDay()
{
    // ... click calendar tab, click a day with entries, verify Day tab is selected
    // and entries for that day are shown
}
```

---

## Component Tree (calendar tab active)

```
TimeEntriesPage.razor
├── Filter tabs (Day | Month | Year | Project | Calendar)
├── CalendarView.razor                    ← NEW (grouping-agnostic)
│   └── <MudTotalCalendar T="TimeEntryCalendarItem">
│       ├── <CellTemplate>                ← custom day cell
│       │   ├── Total hours for the day
│       │   └── Project breakdown (colored bars)
│       └── <TotalTemplate>               ← custom total display
│           └── Formatted hours
├── CalendarDataTransformer               ← NEW (extracted transform logic)
│   ├── ToCalendarItems(entries) → List<CalendarItem>
│   └── ToDailyProjectValues(entries) → List<Value>
├── Summary card (when not calendar tab)
├── Grouped list (when not calendar tab)
├── FAB (always visible)
└── EntrySheet (always conditional)
```

---

## Mobile Considerations

- `Heron.MudTotalCalendar` inherits MudBlazor's responsive grid behavior
- Day cells shrink on narrow viewports (standard MudBlazor responsive behavior)
- The calendar is tested in the project's Playwright tests which use `iPhone 14` viewport (`390x844`) — this already validates mobile layout
- Bottom nav stays fixed; calendar scrolls in the content area above the spacer

---

## Effort Estimate

| Step | Effort |
|------|--------|
| 1. Add NuGet packages | 5 min |
| 2. Add @using statements | 1 min |
| 3. CSS/JS references (if needed) | 10 min |
| 4. Create CalendarDataTransformer service | 30 min |
| 5. Create CalendarView component | 2 hours |
| 6. Integrate calendar tab into TimeEntriesPage | 1 hour |
| 7. Year boundary handling | 30 min |
| 8. Update mock service | 15 min |
| 9. Tests | 1 hour |

**Total: ~5-6 hours (1 day)**

---

## Validation Checklist

- [ ] Calendar tab appears alongside Day/Month/Year/Project
- [ ] Monthly grid renders with correct days for the current month
- [ ] Each day cell shows total hours logged that day
- [ ] Each day cell shows per-project breakdown with colors
- [ ] Prev/next month navigation works
- [ ] Navigating to a different year reloads data correctly
- [ ] Clicking a day with entries switches to Day tab showing those entries
- [ ] Clicking a day with no entries shows the empty state
- [ ] Day with no entries shows "—" or is visually distinct
- [ ] Mobile viewport (390px) renders correctly
- [ ] Existing Day/Month/Year/Project tabs still work
- [ ] FAB and EntrySheet still work from calendar view
- [ ] Showcase mode works (mock data populates calendar)
- [ ] `dotnet test` — all green
- [ ] Playwright tests — new calendar tests pass
