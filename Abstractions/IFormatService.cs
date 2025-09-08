using Microsoft.AspNetCore.Components.Forms;

namespace BookHeaven.Server.Abstractions;

public interface IFormatService
{
	Task DownloadAndStoreCoverAsync(string url, string dest);
	Task StoreCover(byte[]? image, string dest);
	Task StoreBook(string? sourcePath, string dest);
	Task LoadFromFolder(string path);
	Task<Guid?> LoadFromFilePath(string path);
	Task<Guid?> LoadFromFile(IBrowserFile file);
}