using BookHeaven.Server.Features.Files.DTOs;

namespace BookHeaven.Server.Features.Files.Abstractions;

public interface IImportProgressNotifier
{
    Task PublishAsync(ImportProgressDto progressDto);
    Task<IEnumerable<ImportProgressDto>> GetHistoryAsync();
}