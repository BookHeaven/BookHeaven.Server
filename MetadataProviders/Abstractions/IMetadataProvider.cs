using BookHeaven.Server.Entities;

namespace BookHeaven.Server.MetadataProviders.Abstractions;

public interface IMetadataProvider
{
	Task<List<BookMetadata>> GetMetadataByName(string name);
}