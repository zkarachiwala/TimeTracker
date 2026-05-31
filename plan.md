# TimeTracker — Active Plan

## Phase 4 — External OAuth ✅

Completed on `feature/google-auth`. See `docs/roadmap.md` for summary.

---

## Phase 5 — Client Management ✅

Completed on `feature/client-management`.

- `Client` entity with `Name` (unique) and `DefaultHourlyRate` (nullable, ex GST)
- `Project.ClientId` nullable FK; service blocks deleting a client with active projects
- Clients shared across all users — Admin-only CRUD pages + nav link
- Project form updated with client dropdown (`MyInputSelectNullable` component)
- 12 new tests; 51 total

Run migration before starting app:
```bash
cd TimeTracker.Web
dotnet ef database update --context TimeTrackerDataContext
```

---

## Next: Phase 6 — MudBlazor UI uplift

**Branch:** `feature/mudblazor-ui`

Replace Tailwind + Radzen + QuickGrid with MudBlazor. Mobile-first responsive design.

- `MudLayout` + responsive `MudNavMenu` drawer (phone and desktop)
- `MudDataGrid` replaces QuickGrid
- `MudDialog`, `MudTextField`, `MudSelect`, `MudDatePicker` for forms
- MudBlazor Snackbar replaces any remaining toast usage
- `MudChart` evaluated as replacement for Radzen year chart
- Tailwind CSS and Radzen removed
