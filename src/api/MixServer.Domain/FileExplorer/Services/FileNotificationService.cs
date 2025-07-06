using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Events;
using MixServer.Domain.Utilities;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileNotificationService : INotificationService
{
}

public class FileNotificationService(
    ICallbackService callbackService,
    ILogger<FileNotificationService> logger,
    IServiceProvider serviceProvider,
    ITranscodeCache transcodeCache)
    : NotificationService<FileNotificationService>(logger, serviceProvider), IFileNotificationService
{
    public override void Initialize()
    {
        transcodeCache.TranscodeStatusUpdated += CreateHandler<TranscodeStatusUpdatedEventArgs>(TranscodeCacheOnTranscodeStatusUpdated);
    }
    
    private async Task TranscodeCacheOnTranscodeStatusUpdated(object? sender, IServiceProvider sp, TranscodeStatusUpdatedEventArgs e)
    {
        var fileService = sp.GetRequiredService<IFileService>();

        var (parent, file) = await fileService.GetFileAndFolderAsync(e.Path, CancellationToken.None);
        
        var expectedIndexes = await GetExpectedIndexesAsync(sp, parent, file);
        
        await callbackService.FileExplorerNodeUpdated(expectedIndexes, file, null);
    }

    private async Task<Dictionary<string, int>> GetExpectedIndexesAsync(
        IServiceProvider sp,
        IFileExplorerFolder parent,
        IFileExplorerNode node,
        CancellationToken cancellationToken = default)
    {
        var sorts = await sp.GetRequiredService<IFolderSortRepository>()
            .GetFolderSortsAsync(callbackService.ConnectedUserIds, parent.Node.Path, cancellationToken);

        var expectedIndexes = new Dictionary<string, int>();
        foreach (var (userId, sort) in sorts)
        {
            var list = parent.GenerateSortedChildren<IFileExplorerNode>(sort).ToList();
            var index = list.FindIndex(f => f.Path.RootPath == node.Path.RootPath && f.Path.RelativePath == node.Path.RelativePath);
            expectedIndexes[userId] = index;
        }
        
        return expectedIndexes;
    }
}