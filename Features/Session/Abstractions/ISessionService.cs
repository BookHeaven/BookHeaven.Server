namespace BookHeaven.Server.Features.Session.Abstractions;

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