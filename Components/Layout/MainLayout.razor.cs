using BookHeaven.Server.Constants;
using EpubManager.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using BookHeaven.Server.Interfaces;
using BookHeaven.Server.Localization;

namespace BookHeaven.Server.Components.Layout
{
    public partial class MainLayout
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;
        [Inject] private IFormatService<EpubBook> EpubService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        
        private bool _drawerOpen = true;

        private readonly MudTheme _theme = new()
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
                TertiaryContrastText = "#000000",
                SecondaryContrastText = "#000000",
                WarningContrastText = "#000000",
                TableLines = "#4a5d6d"
            }
        };

        private void DrawerToggle()
        {
            _drawerOpen = !_drawerOpen;
        }

        private async void UploadBook(IBrowserFile? file)
        {
            if (file == null) return;

            Snackbar.Add($"{Translations.UPLOADING_BOOK}...", Severity.Info);
			var id = await EpubService.LoadFromFile(file);
            Snackbar.Clear();
            if (id != null)
            {
                NavigationManager.NavigateTo(Urls.GetBookUrl(id.Value));
            }
            else
            {
				Snackbar.Add(Translations.UPLOADING_BOOK_FAILED, Severity.Error);
            }
        }
    }
}