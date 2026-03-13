using BookHeaven.Server.Features.Settings.DTOs;

namespace BookHeaven.Server.Features.Settings.Abstractions;

public interface ISettingsManagerService
{
    Task<ServerSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(ServerSettings serverSettings);
}