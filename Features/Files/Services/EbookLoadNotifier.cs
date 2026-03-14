using System.Collections.Concurrent;
using BookHeaven.Server.Features.Files.Abstractions;
using BookHeaven.Server.Features.Files.DTOs;

namespace BookHeaven.Server.Features.Files.Services;

public class EbookLoadNotifier : IEbookLoadNotifier
{
    private const int MaxHistorySize = 500;
    public event Action<EbookLoadNotificationDto>? OnNotificationPublished;
    private readonly ConcurrentQueue<EbookLoadNotificationDto> _history = [];
    
    public void Publish(EbookLoadNotificationDto notificationDto)
    {
        _history.Enqueue(notificationDto);
        while (_history.Count > MaxHistorySize)
        {
            _history.TryDequeue(out _);
        }

        OnNotificationPublished?.Invoke(notificationDto);
    }

    public Task<IEnumerable<EbookLoadNotificationDto>> GetHistoryAsync()
    {
        return Task.FromResult<IEnumerable<EbookLoadNotificationDto>>(_history);
    }
}