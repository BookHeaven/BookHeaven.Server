using BookHeaven.Server.Features.Import.DTOs;

namespace BookHeaven.Server.Features.Import.Abstractions;

public interface IImportProgressNotifier
{
    Task PublishAsync(ImportProgressDto progressDto);
    Task<IEnumerable<ImportProgressDto>> GetHistoryAsync();
}