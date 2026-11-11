using BookHeaven.Core.Features.Profiles;
using BookHeaven.Server.Features.Authentication.Abstractions;
using BookHeaven.Server.Features.Authorization.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace BookHeaven.Server.Features.Authentication.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly AuthenticationStateProvider _stateProvider;
    private readonly ISender _sender;

    public AuthenticationService(
        AuthenticationStateProvider stateProvider,
        ISender sender)
    {
        _stateProvider = stateProvider;
        _sender = sender;
    }

    public async Task LoginAsync(string name)
    {
        var getProfile = await _sender.Send(new GetProfileByName.Query(name));
        if(getProfile.IsFailure) return;
        if (_stateProvider is IdentityStateProvider identityStateProvider)
        {
            await identityStateProvider.MarkUserAsAuthenticated(getProfile.Value.ProfileId);
        }
    }
}