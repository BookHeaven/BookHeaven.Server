using BookHeaven.Server.Features.Files.Enums;

namespace BookHeaven.Server.Features.Files.DTOs;

public class EbookLoadNotificationDto
{
    public Guid ItemId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public EbookLoadStatus Status { get; init; } = EbookLoadStatus.Queued;
    public DateTime Date { get; } = DateTime.Now;
}