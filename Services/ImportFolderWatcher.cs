using System.Collections.Concurrent;
using BookHeaven.EpubManager.Epub.Entities;
using BookHeaven.Server.Abstractions;

namespace BookHeaven.Server.Services;

public class ImportFolderWatcher(
    ILogger<ImportFolderWatcher> logger, 
    IFormatService epubService) 
    : BackgroundService
{
    private FileSystemWatcher? _watcher;
    private readonly string _processedPath = Path.Combine(Program.ImportPath, "processed");
    private readonly string _errorPath = Path.Combine(Program.ImportPath, "_error");
    private readonly BlockingCollection<string> _filesToProcess = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        _watcher = new FileSystemWatcher(Program.ImportPath)
        {
            Filter = "*.epub",
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
            catch (Exception)
            {
                break;
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
        if (!e.FullPath.EndsWith(".epub")) return;
        if (e.FullPath.StartsWith(_processedPath, StringComparison.OrdinalIgnoreCase) || e.FullPath.StartsWith(_errorPath, StringComparison.OrdinalIgnoreCase)) return;
        
        _filesToProcess.Add(e.FullPath);
    }
    
    private async Task ProcessFileAsync(string filePath)
    {
        if (!filePath.EndsWith(".epub")) return;
        
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
    
    private static void MoveToFolder(string sourcePath, string destFolder)
    {
        var relativePath = Path.GetRelativePath(Program.ImportPath, sourcePath);
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
               !string.Equals(originalDir.TrimEnd(Path.DirectorySeparatorChar), Program.ImportPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
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