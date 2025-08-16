using BookHeaven.Server.Abstractions;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BookHeaven.Server.Services;

public class SessionService(ProtectedLocalStorage storage) : ISessionService
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
        var result = await storage.GetAsync<T>(key.ToString());
        return result.Success ? result.Value : default;
    }

    public async Task RemoveAsync(SessionKey key)
    {
        await storage.DeleteAsync(key.ToString());
    }
}