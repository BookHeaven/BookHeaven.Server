using BookHeaven.Domain.Shared;
using BookHeaven.Server.Features.Metadata.DTOs;

namespace BookHeaven.Server.Features.Metadata.Abstractions;

public interface IMetadataProvider
{
	Task<Result<List<BookMetadata>>> GetMetadataAsync(MetadataRequest request);
}