using BookHeaven.Server.Entities;

namespace BookHeaven.Server.Abstractions;

public interface ISettingsManagerService
{
    Task<ServerSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(ServerSettings serverSettings);
}