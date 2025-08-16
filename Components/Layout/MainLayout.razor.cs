using BookHeaven.EpubManager.Epub.Entities;
using BookHeaven.Server.Abstractions;
using BookHeaven.Server.Constants;
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
        [Inject] private ISessionService SessionService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;

        private bool _checkingProfile = true;
        private bool _drawerOpen = true;
        
        protected override async Task OnInitializedAsync()
        {
            var profileId = await SessionService.GetAsync<Guid>(SessionKey.SelectedProfileId);
            if (profileId == Guid.Empty)
            {
                NavigationManager.NavigateTo(Urls.Profiles);
            }
            else
            {
                _checkingProfile = false;
                StateHasChanged();
            }
        }

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