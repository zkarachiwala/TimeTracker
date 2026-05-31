namespace TimeTracker.Web.Shared;

public static class ProjectColors
{
    private static readonly string[] Palette =
    [
        "#0068CD", "#1FACF2", "#002F6F", "#2e7d32", "#ed6c02",
        "#6a1b9a", "#00838f", "#c62828", "#4527a0", "#558b2f"
    ];

    public static string ForProject(int projectId) =>
        Palette[(projectId - 1) % Palette.Length];

    public static string ForProjectInitials(int projectId) =>
        ForProject(projectId);
}
