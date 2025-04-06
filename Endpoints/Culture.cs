using Microsoft.AspNetCore.Localization;

namespace BookHeaven.Server.Endpoints;

public static class Culture
{
    public static void MapCultureEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/culture/set", (string? culture, string redirectUri, HttpContext context) =>
        {
            if (culture != null)
            {
                context.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(
                        new RequestCulture(culture, culture)));
            }

            return Results.Redirect(redirectUri);
        });
    }
}