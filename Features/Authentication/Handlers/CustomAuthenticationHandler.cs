using System.Text.Encodings.Web;
using BookHeaven.Server.Features.Authentication.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BookHeaven.Server.Features.Authentication.Handlers;

public class CustomAuthenticationHandler(IOptionsMonitor<CustomAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<CustomAuthOptions>(options, logger, encoder) 
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var profile = new SelectedProfile(Guid.Empty, "");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(profile.ToClaimsPrincipal(), "BookHeaven")));
    }
}

public class CustomAuthOptions : AuthenticationSchemeOptions
{
    // Add any custom options here if needed in the future
}