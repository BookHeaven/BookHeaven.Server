using MudBlazor;

namespace BookHeaven.Server.Components.Layout;

public static class Theme
{
    public static readonly MudTheme Main = new()
    {
        PaletteDark = new()
        {
            AppbarBackground = "#1d202bcc",
            AppbarText = "#a8a8a8",
            Background = "#1d202b",
            DrawerBackground = "#1d202b",
            DrawerText = "#a8a8a8",
            DrawerIcon = "#a8a8a8",
            Surface = "#2c3041",
            TextPrimary = "#ffffff",
            TextSecondary = "#a8a8a8",
            Primary = "#56b4ff",
            Secondary = "#a8a8a8",
            Tertiary = "#3097f3",
            TextDisabled = "#595959",
            LinesDefault = "#515151c2",
            LinesInputs = "#56b4ff",
            ActionDefault = "#56b4ff",
            ActionDisabled = "#595959",
            HoverOpacity = 0.1,
            PrimaryContrastText = "#000000",
            TertiaryContrastText = "#FFFFFF",
            SecondaryContrastText = "#000000",
            WarningContrastText = "#000000",
            TableLines = "#4a5d6d"
        }
    };
}