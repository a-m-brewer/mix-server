using MixServer.FolderIndexer.Domain;

namespace MixServer.FolderIndexer.Data.EF;

public class EfFileIndexerUnitOfWork(IFileIndexerDbContext context) : IFileIndexerUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}