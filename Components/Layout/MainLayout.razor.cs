using BookHeaven.Core.Features.Profiles;
using BookHeaven.Server.Constants;
using BookHeaven.Server.Features.Files.Abstractions;
using BookHeaven.Server.Features.Session.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using BookHeaven.Server.Localization;

namespace BookHeaven.Server.Components.Layout
{
    public partial class MainLayout
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;
        [Inject] private ISessionService SessionService { get; set; } = null!;
        [Inject] private ISender Sender { get; set; } = null!;

        private bool _checkingProfile = true;
        private bool _drawerOpen = true;
        
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
    }
}