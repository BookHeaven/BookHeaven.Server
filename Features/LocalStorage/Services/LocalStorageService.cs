using BookHeaven.Server.Features.LocalStorage.Abstractions;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BookHeaven.Server.Features.LocalStorage.Services;

public class LocalStorageService(ILogger<LocalStorageService> logger, ProtectedLocalStorage storage) : ILocalStorageService
{
    public async Task SetAsync<T>(SessionKey key, T value)
    {
        if (value is null)
        {
            return;
        }
        
        await storage.SetAsync(key.ToString(), value);
    }

    public async Task<T?> GetAsync<T>(SessionKey key)
    {
        try
        {
            var result = await storage.GetAsync<T>(key.ToString());
            return result.Success ? result.Value : default;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting session value for key {Key}, might be expected", key);
            return default;
        }
        
    }

    public async Task RemoveAsync(SessionKey key)
    {
        try
        {
            await storage.DeleteAsync(key.ToString());
        }
        catch {}
    }
}