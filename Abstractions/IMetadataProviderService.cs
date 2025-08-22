using BookHeaven.Server.Entities;

namespace BookHeaven.Server.Abstractions;

public interface IMetadataProviderService
{
	Task<List<BookMetadata>> GetMetadataByName(string name);
}