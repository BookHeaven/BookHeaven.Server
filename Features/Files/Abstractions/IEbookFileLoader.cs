using Microsoft.AspNetCore.Components.Forms;

namespace BookHeaven.Server.Features.Files.Abstractions;

public interface IEbookFileLoader
{
	Task<Guid?> LoadFromFilePath(string path);
	Task<Guid?> LoadFromFile(IBrowserFile file);
}