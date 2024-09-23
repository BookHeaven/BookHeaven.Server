using BookHeaven.Server.Entities;
namespace BookHeaven.Server.Interfaces
{
	public interface IMetadataProviderService
	{
		Task<List<BookMetadata>> GetMetadataByName(string name);
	}
}
