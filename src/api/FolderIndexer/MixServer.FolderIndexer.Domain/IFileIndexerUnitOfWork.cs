namespace MixServer.FolderIndexer.Domain;

public interface IFileIndexerUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}