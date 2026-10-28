using BookHeaven.Core.Features.BooksProgress;
using BookHeaven.Server.Features.Api.Abstractions;

namespace BookHeaven.Server.Features.Api.Endpoints.BooksProgress;

public static class ApiGetAllBooksProgressByProfile
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/progress/{profileId:guid}", ApiHandler)
                .WithName("GetAllBooksProgressByProfile")
                .WithTags("Book Progress")
                .WithSummary("Get all books progress by profile")
                .WithDescription("Retrieves the reading progress of every book for a given profile.")
                .Produces<List<BookProgress>>();
        }

        private static async Task<IResult> ApiHandler(Guid profileId, ISender sender)
        {
            var getProgresses = await sender.Send(new GetAllBooksProgressByProfile.Query(profileId));
            return Results.Ok(getProgresses.IsSuccess ? getProgresses.Value : []);
        }
    }
}
