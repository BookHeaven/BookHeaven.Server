using System.Security.Claims;
using BookHeaven.Core.Features.Profiles;
using BookHeaven.Server.Features.Authentication.Models;
using BookHeaven.Server.Features.LocalStorage.Abstractions;
using Microsoft.AspNetCore.Components.Authorization;

namespace BookHeaven.Server.Features.Authorization.Services;

public class IdentityStateProvider(
    ILocalStorageService localStorageService,
    ISender sender) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var profileId = await localStorageService.GetAsync<Guid>(SessionKey.SelectedProfileId);
        var getProfile = await sender.Send(new GetProfileById.Query(profileId));
        if (getProfile.IsFailure)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        var profile = getProfile.Value;
        return new AuthenticationState(new SelectedProfile(profile.ProfileId.ToString(), profile.Name).ToClaimsPrincipal());
    }
    
    public async Task MarkUserAsAuthenticated(Guid profileId)
    {
        await localStorageService.SetAsync(SessionKey.SelectedProfileId, profileId);
        var getProfile = await sender.Send(new GetProfileById.Query(profileId));
        if (getProfile.IsFailure)
        {
            return;
        }
        var profile = getProfile.Value;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new SelectedProfile(profile.ProfileId.ToString(), profile.Name).ToClaimsPrincipal())));
    }
}