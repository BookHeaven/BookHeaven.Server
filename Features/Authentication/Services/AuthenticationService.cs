using BookHeaven.Core.Features.Profiles;
using BookHeaven.Server.Features.Authentication.Abstractions;
using BookHeaven.Server.Features.Authentication.Models;
using BookHeaven.Server.Features.Authorization.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace BookHeaven.Server.Features.Authentication.Services;

public class AuthenticationService(
    AuthenticationStateProvider stateProvider,
    ISender sender)
    : IAuthenticationService
{
    public async Task<SelectedProfile?> GetSelectedProfileAsync()
    {
        try
        {
            var state = await stateProvider.GetAuthenticationStateAsync();
            if(state.User.Identity?.IsAuthenticated == false)
            {
                return null;
            }

            var profile = state.User.FromClaimsPrincipal();
            return profile;
        }
        catch
        {
            return null;
        }
    }
    
    public async Task LoginAsync(string name)
    {
        var getProfile = await sender.Send(new GetProfileByName.Query(name));
        if(getProfile.IsFailure) return;
        if (stateProvider is IdentityStateProvider identityStateProvider)
        {
            await identityStateProvider.MarkUserAsAuthenticated(getProfile.Value.ProfileId);
        }
    }
}