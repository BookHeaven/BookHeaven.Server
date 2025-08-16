using BookHeaven.Domain.Entities;

namespace BookHeaven.Server.Abstractions;

public enum SessionKey
{
    SelectedProfileId
}

public interface ISessionService
{
    Task SetAsync<T>(SessionKey key, T value);
    Task<T?> GetAsync<T>(SessionKey key);
    Task RemoveAsync(SessionKey key);
}