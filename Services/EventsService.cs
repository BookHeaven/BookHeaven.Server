namespace BookHeaven.Server.Services;

public class EventsService
{
    public event Action<Guid>? OnBookAdded;
    public event Action<Guid>? OnBookUpdated;
    public event Action<Guid>? OnBookDeleted;
    
    public void NotifyBookAdded(Guid bookId) => OnBookAdded?.Invoke(bookId);
    public void NotifyBookUpdated(Guid bookId) => OnBookUpdated?.Invoke(bookId);
    public void NotifyBookDeleted(Guid bookId) => OnBookDeleted?.Invoke(bookId);
}