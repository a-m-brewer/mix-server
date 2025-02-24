using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.SignalR.Events;

public class MediaInfoUpdatedDto
{
    public required List<MediaInfoDto> MediaInfo { get; init; }
}