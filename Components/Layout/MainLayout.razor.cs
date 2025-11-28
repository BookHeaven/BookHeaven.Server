using BookHeaven.Domain.Features.Profiles;
using BookHeaven.Server.Abstractions;
using BookHeaven.Server.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using BookHeaven.Server.Localization;
using MediatR;

namespace BookHeaven.Server.Components.Layout
{
    public partial class MainLayout
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;
        [Inject] private IEbookFileLoader EpubService { get; set; } = null!;
        [Inject] private ISessionService SessionService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private ISender Sender { get; set; } = null!;

        private bool _checkingProfile = true;
        private bool _drawerOpen = true;
        private bool _uploadingBooks;
        
        protected override async Task OnInitializedAsync()
        {
            var getProfiles = await Sender.Send(new GetAllProfiles.Query());
            
            var profileId = await SessionService.GetAsync<Guid>(SessionKey.SelectedProfileId);
            if (getProfiles.Value.Count == 0 || profileId == Guid.Empty || getProfiles.Value.All(p => p.ProfileId != profileId))
            {
                await SessionService.RemoveAsync(SessionKey.SelectedProfileId);
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

        private async Task UploadBooks(IReadOnlyList<IBrowserFile>? files)
        {
            if (files is null || files.Count == 0) return;

            _uploadingBooks = true;
            StateHasChanged();
            foreach (var file in files)
            {
                var id = await EpubService.LoadFromFile(file);
                if (id is null)
                {
                    Snackbar.Add(Translations.UPLOADING_BOOK_FAILED + $" '{file.Name}'", Severity.Error);
                }
                else
                {
                    Snackbar.Add(Translations.UPLOADING_BOOK_SUCCESS + $" '{file.Name}'", Severity.Success);
                }
            }
            _uploadingBooks = false;
            StateHasChanged();
        }
    }
}