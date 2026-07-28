using BookHeaven.Domain.Shared;
using BookHeaven.Server.Features.Metadata.DTOs;

namespace BookHeaven.Server.Features.Metadata.Abstractions;

public interface IImageProvider
{
    Task<Result<List<string>>> GetCoversAsync(MetadataRequest request, CancellationToken cancellationToken = default);
}