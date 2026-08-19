using System.Collections.Concurrent;
using BookHeaven.Core;
using BookHeaven.Server.Features.Files.Abstractions;
using BookHeaven.Server.Features.Files.DTOs;
using BookHeaven.Server.Features.Files.Enums;

namespace BookHeaven.Server.Features.Files.Services;

public class ImportFolderWatcher(
    ILogger<ImportFolderWatcher> logger, 
    IEbookFileLoader epubService,
    IEbookLoadNotifier loadNotifier) 
    : BackgroundService
{
    private static readonly string ImportPath = Path.Combine(Directory.GetCurrentDirectory(), "import");
    
    private FileSystemWatcher? _watcher;
    private readonly string _processedPath = Path.Combine(ImportPath, "processed");
    private readonly string _errorPath = Path.Combine(ImportPath, "_error");
    private readonly BlockingCollection<(Guid FileId, string Path)> _filesToProcess = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        if (!Directory.Exists(ImportPath))
        {
            logger.LogWarning("Import folder doesn't exist, can't start folder watcher service.");
            return;
        }
        
        var existingFiles = Directory.EnumerateFiles(ImportPath, "*.*", SearchOption.AllDirectories);
        foreach (var file in existingFiles)
        {
            AddFileToQueue(file, Path.GetFileName(file));
        }

        _watcher = new FileSystemWatcher(ImportPath)
        {
            Filter = "*.*",
            EnableRaisingEvents = true,
            IncludeSubdirectories = true
        };
        _watcher.Created += OnCreated;

        stoppingToken.Register(() =>
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _filesToProcess.CompleteAdding();
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid? fileId;
            string? filePath;
            try
            {
                (fileId, filePath) = _filesToProcess.Take(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error taking file from processing queue. Exiting import service.");
                return;
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;

            while (!IsFileReady(filePath))
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5), stoppingToken);
            }
            await ProcessFileAsync(fileId.Value, filePath);
        }
    }
    
    private static bool IsFileReady(string filePath)
    {
        try
        {
            using var inputStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return inputStream.Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        AddFileToQueue(e.FullPath, e.Name);
    }

    private void AddFileToQueue(string path, string? fileName)
    {
        if (IsExcludedDirectory(path)) return;
        if(!CoreGlobals.SupportedFormats.Any(f => Path.GetExtension(path).Equals(f, StringComparison.OrdinalIgnoreCase))) return;

        var id = Guid.NewGuid();
        _filesToProcess.Add((id, path));
        loadNotifier.Publish(new EbookLoadNotificationDto
        {
            ItemId = id,
            FileName = fileName ?? Path.GetFileName(path),
        });
    }

    private async Task ProcessFileAsync(Guid fileId, string filePath)
    {
        Guid? ebookId = null;
        
        var fileName = Path.GetFileName(filePath);
        loadNotifier.Publish(new EbookLoadNotificationDto
        {
            ItemId = fileId,
            FileName = fileName,
            Status = EbookLoadStatus.InProgress
        });
        logger.LogDebug("Loading '{FileName}' from import path", fileName);

        try
        {
            ebookId = await epubService.LoadFromFilePath(filePath);
            logger.LogDebug("Loaded '{FileName}'.", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Book '{FileName}' could not be imported", fileName);
        }
        
        MoveToFolder(filePath, ebookId is not null ? _processedPath : _errorPath);
        
        CleanUpEmptyDirectories(filePath);
        
        loadNotifier.Publish(new EbookLoadNotificationDto
        {
            ItemId = fileId,
            FileName = fileName,
            Status = ebookId is not null ? EbookLoadStatus.Success : EbookLoadStatus.Failed
        });
    }
    
    private static void MoveToFolder(string sourcePath, string destFolder)
    {
        var relativePath = Path.GetRelativePath(ImportPath, sourcePath);
        var destPath = Path.Combine(destFolder, relativePath);
        var destDir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(destDir))
        {
            Directory.CreateDirectory(destDir);
        }
        File.Move(sourcePath, destPath, overwrite: true);
    }
    
    private static void CleanUpEmptyDirectories(string startPath)
    {
        var originalDir = Path.GetDirectoryName(startPath);
        while (!string.IsNullOrEmpty(originalDir) &&
               !string.Equals(originalDir.TrimEnd(Path.DirectorySeparatorChar), ImportPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            if (Directory.Exists(originalDir) && Directory.GetFileSystemEntries(originalDir).Length == 0)
            {
                Directory.Delete(originalDir);
                originalDir = Path.GetDirectoryName(originalDir);
            }
            else
            {
                break;
            }
        }
    }

    private bool IsExcludedDirectory(string path)
    {
        return path.StartsWith(_processedPath, StringComparison.OrdinalIgnoreCase) 
               || path.StartsWith(_errorPath, StringComparison.OrdinalIgnoreCase);
    }
}