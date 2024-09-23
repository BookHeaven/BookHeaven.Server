using EpubManager.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using BookHeaven.Server.Interfaces;

namespace BookHeaven.Server.Components.Layout
{
    public partial class MainLayout
    {
        [Inject] NavigationManager NavigationManager { get; set; } = null!;
        [Inject] IFormatService<EpubBook> EpubService { get; set; } = null!;
        [Inject] ISnackbar Snackbar { get; set; } = null!;

        bool _drawerOpen = true;

		readonly MudTheme _theme = new()
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
                TertiaryContrastText = "#000000",
                SecondaryContrastText = "#000000",
                TableLines = "#4a5d6d"
            }
        };

        void DrawerToggle()
        {
            _drawerOpen = !_drawerOpen;
        }

        async void UploadBook(IBrowserFile file)
        {
            Snackbar.Add("Uploading book...", Severity.Info);
			Guid? id = await EpubService.LoadFromFile(file);
            if (id != null)
            {
                NavigationManager.NavigateTo($"/book/{id}");
            }
            else
            {
                Snackbar.Clear();
				Snackbar.Add("Failed to upload book", Severity.Error);
            }
        }
    }
}