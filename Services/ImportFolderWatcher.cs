using System.Collections.Concurrent;
using BookHeaven.EpubManager.Epub.Entities;
using BookHeaven.Server.Abstractions;

namespace BookHeaven.Server.Services;

public class ImportFolderWatcher(
    ILogger<ImportFolderWatcher> logger, 
    IFormatService<EpubBook> epubService) 
    : BackgroundService
{
    private FileSystemWatcher? _watcher;
    private readonly string _processedPath = Path.Combine(Program.ImportPath, "processed");
    private readonly BlockingCollection<string> _filesToProcess = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        _watcher = new FileSystemWatcher(Program.ImportPath)
        {
            Filter = "*.epub",
            EnableRaisingEvents = true
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
    
    private bool IsFileReady(string filePath)
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
        if(!e.FullPath.EndsWith(".epub")) return;
        
        _filesToProcess.Add(e.FullPath);
    }
    
    private async Task ProcessFileAsync(string filePath)
    {
        if (!filePath.EndsWith(".epub")) return;
        
        var fileName = Path.GetFileName(filePath);
        logger.LogInformation($"Loading '{fileName}' from import path.");
        var id = await epubService.LoadFromFilePath(filePath);
        if (id != null)
        {
            logger.LogInformation($"Loaded '{fileName}'.");
            Directory.CreateDirectory(_processedPath);
            var destPath = Path.Combine(_processedPath, fileName!);
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }
            File.Move(filePath, destPath);
        }
        else
        {
            logger.LogError($"Failed to load '{fileName}'.");
        }
    }
}