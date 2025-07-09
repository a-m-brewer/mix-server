using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetFolderScanStatus;

public class GetFolderScanStatusQueryHandler(IFolderScanTrackingStore folderScanTrackingStore) : IQueryHandler<FolderScanStatusDto>
{
    public Task<FolderScanStatusDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new FolderScanStatusDto
        {
            ScanInProgress = folderScanTrackingStore.ScanInProgress
        });
    }
}