using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Services.Indexing;
using MixServer.Domain.FileExplorer.Settings;

namespace MixServer.Services;

public class FileSystemIndexerBackgroundService(
    IFileSystemScannerService scanner,
    IOptions<RootFolderSettings> rootFolder) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var path in rootFolder.Value.ChildrenSplit)
        {
            await scanner.ScanAsync(path, stoppingToken);
        }
    }
}