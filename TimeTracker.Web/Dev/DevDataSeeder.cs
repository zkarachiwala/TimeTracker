using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Shared.Entities;
using TimeTracker.Web.Data;

namespace TimeTracker.Web.Dev;

public static class DevDataSeeder
{
    public static async Task<string> SeedAsync(
        IDbContextFactory<TimeTrackerDataContext> contextFactory,
        UserManager<User> userManager)
    {
        var user = userManager.Users.FirstOrDefault()
            ?? throw new InvalidOperationException("No users found. Create an account first, then seed.");

        await using var ctx = await contextFactory.CreateDbContextAsync();

        if (ctx.Clients.Any())
            return "Already seeded — database is not empty. No changes made.";

        // ── Clients ──────────────────────────────────────────────────────────
        var clients = new[]
        {
            new Client { Name = "Acme Corp",            DefaultHourlyRate = 185, ContactName = "Dana Whitfield",  ContactEmail = "dana@acmecorp.com",      ContactPhone = "+61 2 9000 1100", IsArchived = false },
            new Client { Name = "Zenith Health",         DefaultHourlyRate = 195, ContactName = "Dr. Priya Nair", ContactEmail = "p.nair@zenithhealth.org", ContactPhone = "+61 3 9555 2200", IsArchived = false },
            new Client { Name = "Northwind Logistics",   DefaultHourlyRate = 170, ContactName = "Marco Reyes",    ContactEmail = "marco@northwind.io",      ContactPhone = "+61 7 3100 4400", IsArchived = false },
            new Client { Name = "DZK Consulting",        DefaultHourlyRate = null, ContactName = "Zak Karachiwala", ContactEmail = "zak@dzk.com.au",        ContactPhone = "+61 4 1200 8800", IsArchived = false },
            new Client { Name = "Meridian Bank",         DefaultHourlyRate = 210, ContactName = "Helen Cho",      ContactEmail = "h.cho@meridianbank.com",  ContactPhone = "+61 2 8200 6600", IsArchived = true  },
        };
        ctx.Clients.AddRange(clients);
        await ctx.SaveChangesAsync();

        var (acme, zenith, northwind, dzk, meridian) =
            (clients[0], clients[1], clients[2], clients[3], clients[4]);

        // ── Projects ─────────────────────────────────────────────────────────
        Project MakeProject(string name, Client client, string desc, string startStr, string? endStr, decimal rate) =>
            new()
            {
                Name = name,
                ClientId = client.Id,
                HourlyRate = rate,
                Description = desc,
                StartDate = DateTime.Parse(startStr),
                EndDate = endStr is null ? null : DateTime.Parse(endStr),
                ProjectUsers = [new ProjectUser { UserId = user.Id }],
            };

        var projects = new[]
        {
            MakeProject("Acme Corp — Platform Rebuild",      acme,     "Migration of the legacy asset-management system to a .NET 10 + Blazor stack.",          "2026-02-02", null,         185),
            MakeProject("Zenith Health — Data Warehouse",    zenith,   "Azure Synapse data warehouse + Power BI reporting layer for clinical metrics.",          "2026-03-16", null,         195),
            MakeProject("Northwind Logistics — Mobile App",  northwind,"Driver-facing PWA for route tracking and proof-of-delivery capture.",                   "2026-01-12", null,         170),
            MakeProject("Internal — DZK Ops",               dzk,      "Internal tooling, admin, business development and proposals.",                          "2026-01-01", null,           0),
            MakeProject("Meridian Bank — Audit Portal",      meridian, "Compliance audit evidence portal. Phase 1 discovery complete.",                         "2025-11-03", "2026-04-30", 210),
        };
        ctx.Projects.AddRange(projects);
        await ctx.SaveChangesAsync();

        var (p1, p2, p3, p4, p5) =
            (projects[0], projects[1], projects[2], projects[3], projects[4]);

        // ── Time entries ─────────────────────────────────────────────────────
        DateTime At(int dayOffset, int h, int m) =>
            DateTime.Today.AddDays(dayOffset).AddHours(h).AddMinutes(m);

        TimeEntry E(int dayOffset, Project proj, int sh, int sm, int? eh, int? em, string note) =>
            new()
            {
                ProjectId = proj.Id,
                UserId = user.Id,
                Start = At(dayOffset, sh, sm),
                End = eh is null ? null : At(dayOffset, eh.Value, em!.Value),
                Note = note,
            };

        var entries = new[]
        {
            // Today
            E( 0, p1,  9,  0, 10, 30, "Standup + sprint planning"),
            E( 0, p1, 10, 45, 12, 30, "Auth service refactor"),
            E( 0, p2, 13, 30, 15,  0, "Synapse pipeline review"),
            // Yesterday
            E(-1, p3,  8, 30, 11,  0, "PoD capture screen"),
            E(-1, p1, 11, 30, 13,  0, "Code review"),
            E(-1, p4, 14,  0, 15, 15, "Proposal — Lumen Retail"),
            E(-1, p2, 15, 30, 17, 45, "Power BI dataset modelling"),
            // -2
            E(-2, p1,  9,  0, 12, 30, "EF Core migration debugging"),
            E(-2, p2, 13, 30, 16,  0, "Stakeholder workshop"),
            // -3
            E(-3, p3,  9,  0, 12,  0, "Offline sync logic"),
            E(-3, p3, 13,  0, 17,  0, "Map SDK integration"),
            // -4
            E(-4, p1,  9, 30, 13,  0, "API endpoints — time entries"),
            E(-4, p4, 14,  0, 16,  0, "Internal — recruiting"),
            // -5
            E(-5, p2,  9,  0, 12, 30, "Clinical metrics ETL"),
            E(-5, p1, 13, 30, 18,  0, "Blazor UI work"),
            // Older spread for month/year totals
            E( -8, p1,  9,  0, 17,  0, ""),
            E( -9, p2,  9,  0, 16, 30, ""),
            E(-10, p3,  9,  0, 17, 30, ""),
            E(-12, p1,  9,  0, 17,  0, ""),
            E(-15, p2,  9,  0, 15,  0, ""),
            E(-18, p3,  9,  0, 17,  0, ""),
            E(-22, p1,  9,  0, 16,  0, ""),
            E(-26, p4, 10,  0, 14,  0, ""),
        };
        ctx.TimeEntries.AddRange(entries);
        await ctx.SaveChangesAsync();

        return $"Seeded: 5 clients, 5 projects, {entries.Length} time entries for user '{user.UserName}'.";
    }
}
