using System.Xml.Linq;
using BookHeaven.Domain.Extensions;
using BookHeaven.Domain.Features.Books;
using BookHeaven.Server.Features.Api.Abstractions;
using MediatR;

namespace BookHeaven.Server.Features.Api.Endpoints.Opds;

public static class OpdsV1
{
    public class Endpoint : IOpdsEndpoint
    {
        public void MapOpdsEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/v1", Handler)
                .WithSummary("OPDS Catalog v1")
                .WithTags("OPDS")
                .WithDescription("Retrieves the book catalog in OPDS format (version 1).");
        }


        private static async Task<IResult> Handler(
            ISender sender,
            IHttpContextAccessor httpContextAccessor)
        {
            var getBooks = await sender.Send(new GetAllBooks.Query());
            if (getBooks.IsFailure)
            {
                return Results.InternalServerError("Failed to retrieve books");
            }
            
            var request = httpContextAccessor.HttpContext?.Request;
            var baseUrl = request is not null ? $"{request.Scheme}://{request.Host}{request.PathBase}" : "";
            
            var books = getBooks.Value;
            XNamespace opds = "http://opds-spec.org/2010/catalog";
            XNamespace atom = "http://www.w3.org/2005/Atom";
            XNamespace dc = "http://purl.org/dc/terms/";

            var latestUpdate = books.Max(b => b.UpdatedAt).ToUniversalTime();
            
            var feed = new XDocument(
                new XElement(atom + "feed",
                    new XAttribute(XNamespace.Xmlns + "opds", opds),
                    new XAttribute(XNamespace.Xmlns + "dc", dc),
                    new XElement(atom+"title", "BookHeaven Catalog"),
                    new XElement(atom+"id", "bookheaven-catalog"),
                    new XElement(atom+"updated", latestUpdate.ToString("o")),
                    books.Select(book =>
                        new XElement(atom+"entry",
                            new XElement(atom+"title", book.Title),
                            new XElement(atom+"id", $"urn:uuid:{book.BookId}"),
                            new XElement(atom+"updated", book.UpdatedAt.ToUniversalTime().ToString("o")),
                            new XElement(atom+"author", new XElement(atom+"name", book.Author?.Name ?? "Unknown")),
                            !string.IsNullOrEmpty(book.Description) ? new XElement(atom+"content", new XAttribute("type", "text"), book.Description) : null,
                            new XElement(atom+"link",
                                new XAttribute("href", baseUrl + book.EbookUrl()),
                                new XAttribute("type", book.Format.GetMimeType()),
                                new XAttribute("rel", "http://opds-spec.org/acquisition")),
                            new XElement(atom+"link",
                                new XAttribute("href", baseUrl + book.CoverUrl()),
                                new XAttribute("type", "image/jpeg"),
                                new XAttribute("rel", "http://opds-spec.org/image")),
                            book.Series != null ? new XElement(dc + "series", book.Series.Name) : null,
                            book.SeriesIndex.HasValue ? new XElement(dc + "series_index", book.SeriesIndex.Value) : null
                        )
                    )
                )
            );
            
            // Add last-modified response header
            httpContextAccessor.HttpContext?.Response.Headers.LastModified = latestUpdate.ToString("R");
            
            
            return Results.Content(feed.ToString(), "application/atom+xml");
        }
    }
}