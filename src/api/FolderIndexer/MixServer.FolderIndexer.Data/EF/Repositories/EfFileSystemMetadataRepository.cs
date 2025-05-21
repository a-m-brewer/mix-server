using MixServer.FolderIndexer.Domain.Entities;
using MixServer.FolderIndexer.Domain.Repositories;

namespace MixServer.FolderIndexer.Data.EF.Repositories;

public class EfFileSystemMetadataRepository(IFolderIndexerDbContext context) : IFileSystemMetadataRepository
{
    public void Remove(FileMetadataEntity metadata)
    {
        context.FileMetadata.Remove(metadata);
    }

    public async Task AddAsync(FileMetadataEntity metadata, CancellationToken cancellationToken)
    {
        await context.FileMetadata.AddAsync(metadata, cancellationToken);
    }
}