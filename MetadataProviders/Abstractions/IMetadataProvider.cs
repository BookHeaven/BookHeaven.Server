using BookHeaven.Domain.Shared;
using BookHeaven.Server.Entities;
using BookHeaven.Server.MetadataProviders.DTO;

namespace BookHeaven.Server.MetadataProviders.Abstractions;

public interface IMetadataProvider
{
	Task<Result<List<BookMetadata>>> GetMetadataAsync(MetadataRequest request);
}