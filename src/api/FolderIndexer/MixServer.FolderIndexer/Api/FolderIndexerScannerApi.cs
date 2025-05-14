using MixServer.FolderIndexer.Persistence.InMemory;

namespace MixServer.FolderIndexer.Api;

public interface IFolderIndexerScannerApi
{
    Task StartScanAsync(string folder, CancellationToken cancellationToken);
}

internal class FolderIndexerScannerApi(FileSystemIndexerChannelStore channelStore) : IFolderIndexerScannerApi
{
    public async Task StartScanAsync(string folder, CancellationToken cancellationToken)
    {
        await channelStore.ScannerChannel.Writer.WriteAsync(folder, cancellationToken);
    }
}