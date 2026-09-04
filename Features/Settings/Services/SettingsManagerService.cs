using System.Text.Json;
using BookHeaven.Core;
using BookHeaven.Server.Features.Settings.Abstractions;
using BookHeaven.Server.Features.Settings.DTOs;
using Microsoft.Extensions.Options;

namespace BookHeaven.Server.Features.Settings.Services;

public class SettingsManagerService(IOptions<CoreOptions> options) : ISettingsManagerService
{
    private const string SettingsFileName = "settings.json";
    
    public async Task<ServerSettings> LoadSettingsAsync()
    {
        try
        {
            var file = await File.ReadAllTextAsync(Path.Combine(options.Value.DatabasePath, SettingsFileName));
            return JsonSerializer.Deserialize<ServerSettings>(file) ?? new();
        }
        catch (Exception)
        {
            return new();
        }
    }
    
    public async Task SaveSettingsAsync(ServerSettings serverSettings)
    {
        var json = JsonSerializer.Serialize(serverSettings);
        await File.WriteAllTextAsync(Path.Combine(options.Value.DatabasePath, SettingsFileName), json);
    }
}