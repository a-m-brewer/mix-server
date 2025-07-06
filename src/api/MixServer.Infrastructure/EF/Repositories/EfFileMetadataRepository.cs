using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfFileMetadataRepository(MixServerDbContext context) : IFileMetadataRepository
{
    public async Task AddRangeAsync(ICollection<AddMediaMetadataRequest> metadata, CancellationToken cancellationToken)
    {
        var newMetadata = metadata
            .Where(s => s.RemovedMetadata is null)
            .Select(s => s.AddedMetadata);
        await context.AddRangeAsync(newMetadata, cancellationToken);

        foreach (var upgradedMetadata in metadata.Where(w => w.RemovedMetadata is not null))
        {
            var fileMetadata = upgradedMetadata.RemovedMetadata!;

            context.Entry(fileMetadata).State = EntityState.Detached;
            context.Entry(upgradedMetadata.AddedMetadata).State = EntityState.Modified;
        }
    }
}