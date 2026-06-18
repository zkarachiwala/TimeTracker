using TimeTracker.Contracts.Features.Clients;
using TimeTracker.Contracts.Features.Projects;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Client.Mock;

public class MockDataStore
{
    private int _nextEntryId;
    private int _nextProjectId;
    private int _nextClientId;

    public List<ClientResponse> Clients { get; }
    public List<ProjectResponse> Projects { get; }
    public List<TimeEntryResponse> TimeEntries { get; }

    public MockDataStore()
    {
        Clients = SeedClients();
        Projects = SeedProjects();
        TimeEntries = SeedEntries(Projects);
        _nextClientId = Clients.Max(c => c.Id) + 1;
        _nextProjectId = Projects.Max(p => p.Id) + 1;
        _nextEntryId = TimeEntries.Max(e => e.Id) + 1;
    }

    public int NextEntryId() => _nextEntryId++;
    public int NextProjectId() => _nextProjectId++;
    public int NextClientId() => _nextClientId++;

    private static List<ClientResponse> SeedClients() =>
    [
        new(1, "Acme Corp",    false, 150m, null, "Jane Smith", "jane@acme.example",      "+1 555-0101"),
        new(2, "Beta Digital", false, 175m, null, "Tom Lee",    "tom@betadigital.example", "+1 555-0202"),
    ];

    private static List<ProjectResponse> SeedProjects() =>
    [
        new(1, "Website Redesign", 1, "Acme Corp",    150m, "Full site refresh",               new DateTime(2026, 1, 1), null),
        new(2, "API Integration",  1, "Acme Corp",    150m, "REST API integration layer",       new DateTime(2026, 2, 1), null),
        new(3, "Mobile App",       2, "Beta Digital", 175m, "Cross-platform mobile app",        new DateTime(2026, 3, 1), null),
        new(4, "Admin & Overhead", null, null,        null, "Internal admin and overhead time",  null,                   null),
    ];

    private static List<TimeEntryResponse> SeedEntries(List<ProjectResponse> projects)
    {
        var today = DateTime.Today;
        var entries = new List<TimeEntryResponse>();
        int id = 1;

        TimeEntryResponse E(int projectId, DateTime start, int minutes, string? invoice = null)
        {
            var p = projects.First(x => x.Id == projectId);
            var result = new TimeEntryResponse(
                id,
                new ProjectSummary(p.Id, p.Name),
                start,
                start.AddMinutes(minutes),
                null,
                invoice,
                invoice != null ? (DateTime?)start.Date : null);
            id++;
            return result;
        }

        // Today
        entries.Add(E(1, today.AddHours(9),   120));
        entries.Add(E(1, today.AddHours(13),   90));

        // Yesterday
        entries.Add(E(3, today.AddDays(-1).AddHours(9),  150));
        entries.Add(E(1, today.AddDays(-1).AddHours(14),  90));

        // 2 days ago
        entries.Add(E(2, today.AddDays(-2).AddHours(10), 120));

        // 5 days ago
        entries.Add(E(1, today.AddDays(-5).AddHours(9),  180));
        entries.Add(E(3, today.AddDays(-5).AddHours(14), 120));

        // 7 days ago
        entries.Add(E(2, today.AddDays(-7).AddHours(10), 150, "INV-2026-005"));
        entries.Add(E(3, today.AddDays(-7).AddHours(14),  90));

        // 10 days ago
        entries.Add(E(1, today.AddDays(-10).AddHours(9),  90));
        entries.Add(E(4, today.AddDays(-10).AddHours(11), 60));

        // 14 days ago
        entries.Add(E(2, today.AddDays(-14).AddHours(9),  180, "INV-2026-005"));
        entries.Add(E(3, today.AddDays(-14).AddHours(14), 120));

        // 17 days ago
        entries.Add(E(1, today.AddDays(-17).AddHours(9),  150));
        entries.Add(E(2, today.AddDays(-17).AddHours(14),  90));

        // 21 days ago
        entries.Add(E(1, today.AddDays(-21).AddHours(9),  120));
        entries.Add(E(3, today.AddDays(-21).AddHours(13), 180));

        // 28 days ago
        entries.Add(E(1, today.AddDays(-28).AddHours(9),  150, "INV-2026-004"));
        entries.Add(E(2, today.AddDays(-28).AddHours(13), 120, "INV-2026-004"));
        entries.Add(E(3, today.AddDays(-28).AddHours(16),  60));

        // 35 days ago
        entries.Add(E(2, today.AddDays(-35).AddHours(9),  180, "INV-2026-004"));
        entries.Add(E(3, today.AddDays(-35).AddHours(14), 150, "INV-2026-004"));

        // 42 days ago
        entries.Add(E(1, today.AddDays(-42).AddHours(9),  120));
        entries.Add(E(2, today.AddDays(-42).AddHours(13), 120));

        // 49 days ago
        entries.Add(E(1, today.AddDays(-49).AddHours(9),  90));
        entries.Add(E(3, today.AddDays(-49).AddHours(11), 150));

        // 56 days ago
        entries.Add(E(2, today.AddDays(-56).AddHours(9),  180, "INV-2026-003"));
        entries.Add(E(4, today.AddDays(-56).AddHours(14),  60));

        // 63 days ago
        entries.Add(E(1, today.AddDays(-63).AddHours(9),  120, "INV-2026-003"));
        entries.Add(E(2, today.AddDays(-63).AddHours(12), 120, "INV-2026-003"));
        entries.Add(E(3, today.AddDays(-63).AddHours(15),  90));

        // 70 days ago
        entries.Add(E(1, today.AddDays(-70).AddHours(9),  150, "INV-2026-003"));
        entries.Add(E(3, today.AddDays(-70).AddHours(14), 120));

        // 84 days ago
        entries.Add(E(2, today.AddDays(-84).AddHours(9),  180, "INV-2026-002"));
        entries.Add(E(3, today.AddDays(-84).AddHours(14), 150, "INV-2026-002"));

        // 98 days ago
        entries.Add(E(1, today.AddDays(-98).AddHours(9),  120, "INV-2026-002"));
        entries.Add(E(2, today.AddDays(-98).AddHours(13), 120, "INV-2026-002"));

        // 112 days ago
        entries.Add(E(1, today.AddDays(-112).AddHours(9),  180, "INV-2026-001"));
        entries.Add(E(3, today.AddDays(-112).AddHours(13), 150, "INV-2026-001"));
        entries.Add(E(4, today.AddDays(-112).AddHours(16),  60));

        // 126 days ago
        entries.Add(E(2, today.AddDays(-126).AddHours(9),  150, "INV-2026-001"));
        entries.Add(E(3, today.AddDays(-126).AddHours(13), 120, "INV-2026-001"));

        // 140 days ago
        entries.Add(E(1, today.AddDays(-140).AddHours(9),  180, "INV-2026-001"));
        entries.Add(E(2, today.AddDays(-140).AddHours(13), 120, "INV-2026-001"));

        // 154 days ago
        entries.Add(E(1, today.AddDays(-154).AddHours(9),  150, "INV-2026-001"));
        entries.Add(E(3, today.AddDays(-154).AddHours(13), 120, "INV-2026-001"));

        return entries;
    }
}
