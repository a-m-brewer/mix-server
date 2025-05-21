using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Domain.Repositories;

public interface IFileSystemMetadataRepository
{
    void Remove(FileMetadataEntity metadata);
    Task AddAsync(FileMetadataEntity metadata, CancellationToken cancellationToken);
}