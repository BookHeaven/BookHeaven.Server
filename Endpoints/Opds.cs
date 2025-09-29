using System.Xml.Linq;
using BookHeaven.Domain.Extensions;
using BookHeaven.Domain.Features.Books;
using MediatR;

namespace BookHeaven.Server.Endpoints;

public static class Opds
{
    public static IServiceCollection AddOpds(this IServiceCollection services)
    {
        services.AddTransient<Endpoint>();
        return services;
    }

    public static IApplicationBuilder MapOpds(this WebApplication app)
    {
        app.MapGet("/opds", Endpoint.Handler);
        return app;
    }
    
    public class Endpoint
    {
        public static async Task<IResult> Handler(ISender sender)
        {
            var getBooks = await sender.Send(new GetAllBooks.Query());
            if (getBooks.IsFailure)
            {
                return Results.InternalServerError("Failed to retrieve books");
            }
            
            var books = getBooks.Value;
            XNamespace opds = "http://opds-spec.org/2010/catalog";
            XNamespace atom = "http://www.w3.org/2005/Atom";
            XNamespace dc = "http://purl.org/dc/terms/";
            
            var feed = new XDocument(
                new XElement("feed",
                    new XAttribute(XNamespace.Xmlns + "opds", opds),
                    new XAttribute(XNamespace.Xmlns + "atom", atom),
                    new XElement(atom+"title", "BookHeaven Catalog"),
                    new XElement(atom+"id", "urn:bookheaven:catalog"),
                    new XElement(atom+"updated", DateTime.UtcNow.ToString("o")),
                    books.Select(book =>
                        new XElement(atom+"entry",
                            new XElement(atom+"title", book.Title),
                            new XElement(atom+"id", $"urn:bookheaven:book:{book.BookId}"),
                            new XElement(atom+"updated", DateTime.UtcNow.ToString("o")),
                            new XElement(atom+"author", new XElement(atom+"name", book.Author?.Name ?? "Unknown")),
                            !string.IsNullOrEmpty(book.Description) ? new XElement(atom+"content", new XAttribute("type", "text"), book.Description) : null,
                            new XElement(atom+"link",
                                new XAttribute("href", book.EbookUrl()),
                                new XAttribute("type", book.Format.GetMimeType()),
                                new XAttribute("rel", "http://opds-spec.org/acquisition")),
                            new XElement(atom+"link",
                                new XAttribute("href", book.CoverUrl()),
                                new XAttribute("type", "image/jpeg"),
                                new XAttribute("rel", "http://opds-spec.org/image")),
                            book.Series != null ? new XElement(dc + "series", book.Series) : null,
                            book.SeriesIndex.HasValue ? new XElement(dc + "series_index", book.SeriesIndex.Value) : null
                        )
                    )
                )
            );
            
            return Results.Content(feed.ToString(), "application/atom+xml");
        }
    }
}