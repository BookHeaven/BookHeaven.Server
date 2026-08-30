namespace BookHeaven.Server.Features.LocalStorage.Abstractions;

public enum SessionKey
{
    SelectedProfileId
}

public interface ILocalStorageService
{
    Task SetAsync<T>(SessionKey key, T value);
    Task<T?> GetAsync<T>(SessionKey key);
    Task RemoveAsync(SessionKey key);
}