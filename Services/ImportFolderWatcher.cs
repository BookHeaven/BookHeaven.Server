using System.Collections.Concurrent;
using BookHeaven.Domain;
using BookHeaven.Server.Abstractions;

namespace BookHeaven.Server.Services;

public class ImportFolderWatcher(
    ILogger<ImportFolderWatcher> logger, 
    IEbookFileLoader epubService) 
    : BackgroundService
{
    private static readonly string ImportPath = Path.Combine(Directory.GetCurrentDirectory(), "import");
    
    private FileSystemWatcher? _watcher;
    private readonly string _processedPath = Path.Combine(ImportPath, "processed");
    private readonly string _errorPath = Path.Combine(ImportPath, "_error");
    private readonly BlockingCollection<string> _filesToProcess = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        if (!Directory.Exists(ImportPath))
        {
            logger.LogWarning("Import folder doesn't exist, can't start folder watcher service.");
            return;
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
            string? filePath;
            try
            {
                filePath = _filesToProcess.Take(stoppingToken);
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
            await ProcessFileAsync(filePath);
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
        if(!DomainGlobals.SupportedFormats.Any(f => Path.GetExtension(e.FullPath).Equals(f, StringComparison.OrdinalIgnoreCase))) return;
        if (e.FullPath.StartsWith(_processedPath, StringComparison.OrdinalIgnoreCase) || e.FullPath.StartsWith(_errorPath, StringComparison.OrdinalIgnoreCase)) return;

        _filesToProcess.Add(e.FullPath);
    }

    private async Task ProcessFileAsync(string filePath)
    {
        Guid? id = null;
        
        var fileName = Path.GetFileName(filePath);
        logger.LogInformation("Loading '{FileName}' from import path", fileName);

        try
        {
            id = await epubService.LoadFromFilePath(filePath);
            logger.LogInformation("Loaded '{FileName}'.", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Book '{FileName}' could not be imported", fileName);
        }
        
        MoveToFolder(filePath, id is not null ? _processedPath : _errorPath);
        
        CleanUpEmptyDirectories(filePath);
    }
    
    private void MoveToFolder(string sourcePath, string destFolder)
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
    
    private void CleanUpEmptyDirectories(string startPath)
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
}