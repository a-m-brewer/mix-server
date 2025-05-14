using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Settings;
using MixServer.FolderIndexer.Api;

namespace MixServer.Services;

public class MixServerFileIndexerStartupService(
    IOptions<RootFolderSettings> rootFolder,
    IFolderIndexerScannerApi scannerApi) : HostedLifecycleServiceBase
{
    public override async Task StartedAsync(CancellationToken cancellationToken)
    {
        foreach (var folder in rootFolder.Value.ChildrenSplit)
        {
            await scannerApi.StartScanAsync(folder, cancellationToken);
        }
    }
}