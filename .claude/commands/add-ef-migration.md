# /add-ef-migration

Add an EF Core migration for this project's dual-DbContext setup. Always run from `TimeTracker.Web/`.

## Usage

`/add-ef-migration <MigrationName>`

Example: `/add-ef-migration AddInvoiceTable`

## Steps

### 1. Decide which context(s) to migrate

This project has two EF Core contexts, both targeting the same SQL Server database (`TimeTrackerDb`):

| Context | Schema | Contains |
|---------|--------|---------|
| `TimeTrackerDataContext` | `app` | App entities (TimeEntries, Projects, Clients, etc.) |
| `IdentityDataContext` | `id` | ASP.NET Identity tables |

New app entities always go into `TimeTrackerDataContext`. Only touch `IdentityDataContext` for Identity schema changes.

### 2. Add the migration(s)

```bash
cd TimeTracker.Web

# For app schema changes:
dotnet ef migrations add <MigrationName> --context TimeTrackerDataContext

# For identity schema changes (rare):
dotnet ef migrations add <MigrationName> --context IdentityDataContext
```

### 3. Review the generated migration

Check `Migrations/` for the generated `.cs` file. Verify:
- Tables use the correct schema prefix (`app.` or `id.`)
- No accidental drops or renames
- `Down()` method correctly reverses the migration

### 4. Apply to the local database

```bash
dotnet ef database update --context TimeTrackerDataContext
dotnet ef database update --context IdentityDataContext  # only if you also migrated identity
```

## Gotchas

- Always run from `TimeTracker.Web/` — EF Design tools require the startup project context.
- Both contexts target the same database but use separate migration histories (`__EFMigrationsHistory` is schema-qualified).
- If running inside the dev container, the SQL Server instance is the container's Docker DB — confirm the connection string in user secrets is pointing at the right instance.
- After adding a migration, always confirm the generated SQL looks right with `dotnet ef migrations script --context TimeTrackerDataContext` before applying.
