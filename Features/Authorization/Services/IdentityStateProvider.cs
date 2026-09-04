using System.Security.Claims;
using BookHeaven.Core.Features.Profiles;
using BookHeaven.Server.Features.Authentication.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BookHeaven.Server.Features.Authorization.Services;

public class IdentityStateProvider(
    ProtectedLocalStorage protectedLocalStorage,
    ISender sender) : AuthenticationStateProvider
{
    private const string LocalStorageKey = "SelectedProfileId";
    
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var getProfileId = await protectedLocalStorage.GetAsync<Guid>(LocalStorageKey);
        if (!getProfileId.Success || getProfileId.Value == Guid.Empty)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        var getProfile = await sender.Send(new GetProfileById.Query(getProfileId.Value));
        if (getProfile.IsFailure)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        var profile = getProfile.Value;
        return new AuthenticationState(new SelectedProfile(profile.ProfileId, profile.Name).ToClaimsPrincipal());
    }
    
    public async Task MarkUserAsAuthenticated(Guid profileId)
    {
        var getProfile = await sender.Send(new GetProfileById.Query(profileId));
        if (getProfile.IsFailure)
        {
            return;
        }
        await protectedLocalStorage.SetAsync(LocalStorageKey, profileId);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new SelectedProfile(getProfile.Value.ProfileId, getProfile.Value.Name).ToClaimsPrincipal())));
    }
}