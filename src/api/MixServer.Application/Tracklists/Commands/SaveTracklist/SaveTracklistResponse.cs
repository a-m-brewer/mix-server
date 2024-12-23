using MixServer.Domain.Tracklists.Dtos.Import;

namespace MixServer.Application.Tracklists.Commands.SaveTracklist;

public class SaveTracklistResponse
{
    public required ImportTracklistDto Tracklist { get; set; }
}