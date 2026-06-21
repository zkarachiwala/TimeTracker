using MudBlazor;

namespace TimeTracker.Web.Shared.Theme;

public static class DzkTheme
{
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0068CD",
            PrimaryContrastText = "#ffffff",
            Secondary = "#002F6F",
            SecondaryContrastText = "#ffffff",
            Info = "#1FACF2",
            Success = "#2e7d32",
            Warning = "#ed6c02",
            Error = "#d32f2f",
            Background = "#f4f6f9",
            Surface = "#ffffff",
            DrawerBackground = "#ffffff",
            AppbarBackground = "#0068CD",
            AppbarText = "#ffffff",
            TextPrimary = "rgba(0,0,0,0.87)",
            TextSecondary = "rgba(0,0,0,0.60)",
            TextDisabled = "rgba(0,0,0,0.38)",
            Divider = "rgba(0,0,0,0.10)",
            ActionDefault = "rgba(0,0,0,0.54)",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Roboto", "Helvetica Neue", "Helvetica", "Arial", "sans-serif"],
            }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            DrawerWidthLeft = "256px",
            DrawerMiniWidthLeft = "56px",
        }
    };
}
