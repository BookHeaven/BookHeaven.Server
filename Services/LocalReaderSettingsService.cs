using BookHeaven.Core.Features.Reader.Abstractions;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BookHeaven.Server.Services;

public class LocalReaderSettingsService(ProtectedLocalStorage protectedLocalStorage) : IReaderSettingsService
{
    private const string KeyPrefix = "ReaderSettings";
    private static string LocalStorageKey(Guid profileId) => $"{KeyPrefix}_{profileId}";
    
    public ProfileSettings? ReaderSettings { get; private set; }
    public event Action<string?>? OnSettingChanged;
    public void NotifySettingChanged(string? settingName = null)
    {
        OnSettingChanged?.Invoke(settingName);
        _ = SaveSettings();
    }

    public async Task LoadSettings(Guid profileId)
    {
        var getSettings = await protectedLocalStorage.GetAsync<ProfileSettings>(LocalStorageKey(profileId));
        ReaderSettings = getSettings.Value ?? new() { ProfileId = profileId };
    }

    public async Task SaveSettings()
    {
        if(ReaderSettings is null) return;
        await protectedLocalStorage.SetAsync(LocalStorageKey(ReaderSettings.ProfileId), ReaderSettings);
    }
    
    public void Dispose()
    {
        ReaderSettings = null;
        GC.SuppressFinalize(this);
    }
}