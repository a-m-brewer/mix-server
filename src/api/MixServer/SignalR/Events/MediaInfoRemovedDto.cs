using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.SignalR.Events;

public class MediaInfoRemovedDto
{
    public required List<NodePathDto> NodePaths { get; init; }
}