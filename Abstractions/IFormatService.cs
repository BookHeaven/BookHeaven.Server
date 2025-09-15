using Microsoft.AspNetCore.Components.Forms;

namespace BookHeaven.Server.Abstractions;

public interface IFormatService
{
	Task<Guid?> LoadFromFilePath(string path);
	Task<Guid?> LoadFromFile(IBrowserFile file);
}