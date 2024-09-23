using Microsoft.AspNetCore.Components.Forms;

namespace BookHeaven.Server.Interfaces
{
	public interface IFormatService<T>
	{
		Task<T> GetMetadata(string path);
		Task StoreCover(byte[]? image, string dest);
		Task StoreBook(string? sourcePath, string dest);
		Task LoadFromFolder(string path);
		Task<Guid?> LoadFromFilePath(string path);
		Task<Guid?> LoadFromFile(IBrowserFile file);

	}
}
