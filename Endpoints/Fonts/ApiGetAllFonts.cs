using BookHeaven.Domain;
using BookHeaven.Server.Abstractions.Api;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Endpoints.Fonts;

public static class ApiGetAllFonts
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/fonts", Handler)
                .WithName("GetAllFonts")
                .WithTags("Fonts")
                .WithSummary("Get all available fonts")
                .WithDescription("Retrieves a list of all fonts available in the system.")
                .Produces<List<Domain.Entities.Font>>()
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }

        private static async Task<IResult> Handler(
            IDbContextFactory<DatabaseContext> dbContextFactory,
            ILogger<Endpoint> logger)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            var fonts = context.Fonts.ToList();
            return Results.Ok(fonts);
        }
    }
}