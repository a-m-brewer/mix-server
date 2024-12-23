using MixServer.Domain.Tracklists.Dtos.Import;

namespace MixServer.Application.Tracklists.Commands.ImportTracklist;

public class ImportTracklistResponse(ImportTracklistDto tracklist)
{
    public ImportTracklistDto Tracklist { get; } = tracklist;
}