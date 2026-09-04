using System.Security.Claims;

namespace BookHeaven.Server.Features.Authentication.Models;

public record SelectedProfile(Guid ProfileId, string Name);

public static class SelectedProfileExtensions
{
    
    public static ClaimsPrincipal ToClaimsPrincipal(this SelectedProfile profile)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profile.ProfileId.ToString()),
            new(ClaimTypes.Name, profile.Name)
        };

        var identity = new ClaimsIdentity(claims, "BookHeaven");
        return new ClaimsPrincipal(identity);
    }
    
    public static SelectedProfile ToSelectedProfile(this ClaimsPrincipal claimsPrincipal)
    {
        var profileIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
        var nameClaim = claimsPrincipal.FindFirst(ClaimTypes.Name);

        if (profileIdClaim is null || nameClaim is null)
        {
            throw new InvalidOperationException("Invalid ClaimsPrincipal: Missing required claims.");
        }

        return new SelectedProfile(Guid.Parse(profileIdClaim.Value), nameClaim.Value);
    }
}