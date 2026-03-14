using BookHeaven.Server.Features.Files.DTOs;

namespace BookHeaven.Server.Features.Files.Abstractions;

public interface IEbookLoadNotifier
{
    event Action<EbookLoadNotificationDto>? OnNotificationPublished;
    void Publish(EbookLoadNotificationDto notificationDto);
    Task<IEnumerable<EbookLoadNotificationDto>> GetHistoryAsync();
}