using System.Security.Claims;

namespace BookHeaven.Server.Features.Authentication.Models;

public record SelectedProfile(string ProfileId, string Name);

public static class SelectedProfileExtensions
{
    public static ClaimsPrincipal ToClaimsPrincipal(this SelectedProfile profile)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profile.ProfileId),
            new(ClaimTypes.Name, profile.Name)
        };

        var identity = new ClaimsIdentity(claims, "BookHeaven");
        return new ClaimsPrincipal(identity);
    }
    
    public static SelectedProfile FromClaimsPrincipal(this ClaimsPrincipal claimsPrincipal)
    {
        var profileIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
        var nameClaim = claimsPrincipal.FindFirst(ClaimTypes.Name);

        if (profileIdClaim is null || nameClaim is null)
        {
            throw new InvalidOperationException("Invalid ClaimsPrincipal: Missing required claims.");
        }

        return new SelectedProfile(profileIdClaim.Value, nameClaim.Value);
    }
}