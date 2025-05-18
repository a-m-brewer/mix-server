namespace MixServer.FolderIndexer.Interface.Api;

public interface IFolderIndexerScannerApi
{
    Task StartScanAsync(string folder, CancellationToken cancellationToken);
}