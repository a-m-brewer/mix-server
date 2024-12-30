using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Domain.Tracklists.Dtos.Import;

namespace MixServer.Application.Sessions.Responses;

public class PlaybackSessionDto
{
    public Guid Id { get; set; }

    public string FileDirectory { get; set; } = string.Empty;

    public FileExplorerFileNodeResponse File { get; set; } = null!;

    public ImportTracklistDto Tracklist { get; set; } = new();
    
    public DateTime LastPlayed { get; set; }

    public bool Playing { get; set; }
    
    public double CurrentTime { get; set; }

    public Guid? DeviceId { get; set; }

    public bool AutoPlay { get; set; }
}