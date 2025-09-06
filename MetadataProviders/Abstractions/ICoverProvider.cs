using BookHeaven.Domain.Shared;
using BookHeaven.Server.MetadataProviders.DTO;

namespace BookHeaven.Server.MetadataProviders.Abstractions;

public interface ICoverProvider
{
    Task<Result<List<string>>> GetCoversAsync(MetadataRequest request, CancellationToken cancellationToken = default);
}