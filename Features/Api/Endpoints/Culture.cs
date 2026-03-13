using BookHeaven.Server.Features.Api.Abstractions;
using Microsoft.AspNetCore.Localization;

namespace BookHeaven.Server.Features.Api.Endpoints;

public static class Culture
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/culture/set",
                    (string? culture, string redirectUri, HttpContext context) =>
                    {
                        if (culture != null)
                        {
                            context.Response.Cookies.Append(
                                CookieRequestCultureProvider.DefaultCookieName,
                                CookieRequestCultureProvider.MakeCookieValue(
                                    new RequestCulture(culture, culture)));
                        }

                        return Results.Redirect(redirectUri);
                    })
                .ExcludeFromDescription();
        }
    }
}