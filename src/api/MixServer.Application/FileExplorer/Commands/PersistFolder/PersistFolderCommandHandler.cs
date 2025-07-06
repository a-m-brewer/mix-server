using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;

namespace MixServer.Application.FileExplorer.Commands.PersistFolder;

public class PersistFolderCommandHandler(
    IFolderPersistenceService folderPersistenceService,
    IUnitOfWork unitOfWork) : ICommandHandler<PersistFolderCommand>
{
    public async Task HandleAsync(PersistFolderCommand request, CancellationToken cancellationToken = default)
    {
        await folderPersistenceService.AddOrUpdateFolderAsync(request.DirectoryPath, request.Directory, request.Children, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}