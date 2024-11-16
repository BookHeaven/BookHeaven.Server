using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BookHeaven.Server.Features.Books;
public sealed record CreateBookCommand(
    Guid AuthorId,
    Guid? SeriesId,
    decimal? SeriesIndex,
    string Title,
    string? Description,
    DateTime? PublishedDate,
    string? Publisher,
    string? Language,
    string? Isbn10,
    string? Isbn13,
    string? Asin,
    string? Uuid
    ) : ICommand<Guid>;

internal class CreateBookCommandHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : ICommandHandler<CreateBookCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Book book = new()
        {
            Title = request.Title,
            Description = request.Description,
            PublishedDate = request.PublishedDate,
            Publisher = request.Publisher,
            Language = request.Language,
            AuthorId = request.AuthorId,
            SeriesId = request.SeriesId,
            SeriesIndex = request.SeriesIndex
        };
        
        await context.Books.AddAsync(book, cancellationToken);
        
        try 
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            return Result<Guid>.Failure(new("Error", e.Message));
        }

        return Result<Guid>.Success(book.BookId);
    }
}