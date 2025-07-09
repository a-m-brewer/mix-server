using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.Tracklists.Dtos.Import;

namespace MixServer.SignalR.Events;

public class TracklistUpdatedDto
{
    public required NodePathDto Path { get; init; }
    
    public required ImportTracklistDto Tracklist { get; init; }
}