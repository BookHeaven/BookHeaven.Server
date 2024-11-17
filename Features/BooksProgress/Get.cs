using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.BooksProgress;

public sealed record GetBookProgress(Guid BookProgressId) : IQuery<BookProgress>;

internal class GetBookProgressQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<GetBookProgress, BookProgress>
{
    public async Task<Result<BookProgress>> Handle(GetBookProgress request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var progress = await context.BooksProgress.FirstOrDefaultAsync(bp => bp.BookProgressId == request.BookProgressId, cancellationToken);

        return progress == null ? Result<BookProgress>.Failure(new Error("Error", "Progress not found")) : Result<BookProgress>.Success(progress);
    }
}

public sealed record GetBookProgressByProfileQuery(Guid BookId, Guid ProfileId) : IQuery<BookProgress>;

internal class GetBookProgressByProfileQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<GetBookProgressByProfileQuery, BookProgress>
{

    public async Task<Result<BookProgress>> Handle(GetBookProgressByProfileQuery request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var progress = await context.BooksProgress.FirstOrDefaultAsync(x => x.BookId == request.BookId && x.ProfileId == request.ProfileId, cancellationToken);

        return progress == null ? Result<BookProgress>.Failure(new Error("Error", "Progress not found")) : Result<BookProgress>.Success(progress);
    }
}