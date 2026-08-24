using MudBlazor;

namespace SemBroncaAI.Garage.Web.Components.Layout;

internal static class GarageTheme
{
    internal static MudTheme Value { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#F97316",
            Background = "#F7F8FA",
            Surface = "#FFFFFF",
            TextPrimary = "#111827",
            TextSecondary = "#6B7280"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#FB923C",
            Background = "#0F172A",
            Surface = "#172033",
            TextPrimary = "#F8FAFC",
            TextSecondary = "#CBD5E1",
            AppbarBackground = "#111827",
            DrawerBackground = "#111827",
            DrawerText = "#E2E8F0",
            LinesDefault = "#334155",
            Divider = "#334155",
            ActionDefault = "#CBD5E1"
        }
    };
}
