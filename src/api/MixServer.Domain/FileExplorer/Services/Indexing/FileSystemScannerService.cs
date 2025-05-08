using System.Threading.Tasks.Dataflow;

namespace MixServer.Domain.FileExplorer.Services.Indexing;

public interface IFileSystemScannerService
{
    Task ScanAsync(string path, CancellationToken cancellationToken);
}

internal class FileSystemScannerService(FileSystemIndexerChannelStore channelStore) : IFileSystemScannerService
{
    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = 0
    };

    public async Task ScanAsync(string path, CancellationToken cancellationToken)
    {
        var root = new DirectoryInfo(path);

        List<FileSystemInfo> children;
        try
        {
            children = root.EnumerateFileSystemInfos("*", EnumerationOptions).ToList();
        }
        catch
        {
            return;
        }
        
        if (children.Count == 0)
        {
            return;
        }
        
        await channelStore.FileSystemInfoChannel.Writer.WriteAsync((root, children), cancellationToken).ConfigureAwait(false);
        
        var folders = children.OfType<DirectoryInfo>().ToList();
        
        if (folders.Count == 0)
        {
            return;
        }

        var actionBlock = new ActionBlock<int>(async i =>
            {
                await ScanAsync(folders[i].FullName, cancellationToken).ConfigureAwait(false);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });

        for (var i = 0; i < folders.Count; i++)
        {
            await actionBlock.SendAsync(i, cancellationToken).ConfigureAwait(false);
        }

        actionBlock.Complete();

        await actionBlock.Completion.ConfigureAwait(false);
    }
}