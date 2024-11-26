using System.Text.Json;
using BookHeaven.Server.Abstractions;
using BookHeaven.Server.Entities;

namespace BookHeaven.Server.Services;

public class SettingsManagerService : ISettingsManagerService
{
    private const string SettingsFileName = "settings.json";
    
    public async Task<ServerSettings> LoadSettingsAsync()
    {
        try
        {
            var file = await File.ReadAllTextAsync(Path.Combine(Program.DatabasePath, SettingsFileName));
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
        await File.WriteAllTextAsync(Path.Combine(Program.DatabasePath, SettingsFileName), json);
    }
}