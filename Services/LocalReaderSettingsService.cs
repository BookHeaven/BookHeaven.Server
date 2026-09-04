using BookHeaven.Core.Features.Reader.Abstractions;
using BookHeaven.Server.Features.LocalStorage.Abstractions;

namespace BookHeaven.Server.Services;

public class LocalReaderSettingsService(ILocalStorageService localStorageService) : IReaderSettingsService
{
    public ProfileSettings? ReaderSettings { get; private set; }
    public event Action? OnSettingsChanged;
    public void NotifySettingsChanged()
    {
        OnSettingsChanged?.Invoke();
        _ = SaveSettings();
    }

    public async Task LoadSettings(Guid profileId)
    {
        var settings = await localStorageService.GetAsync<ProfileSettings>(LocalStorageKey.ReaderSettings);
        ReaderSettings = settings ?? new() { ProfileId = profileId };
    }

    public async Task SaveSettings()
    {
        if(ReaderSettings is null) return;
        await localStorageService.SetAsync(LocalStorageKey.ReaderSettings, ReaderSettings);
    }
    
    public void Dispose()
    {
        ReaderSettings = null;
        GC.SuppressFinalize(this);
    }
}