﻿using MixServer.Application.FileExplorer.Queries.GetNode;

namespace MixServer.Application.Sessions.Responses;

public class PlaybackSessionDto
{
    public Guid Id { get; set; }

    public string FileDirectory { get; set; } = string.Empty;

    public required FileExplorerFileNodeResponse File { get; init; }
    
    public required StreamKeyDto StreamKey { get; init; }
    
    public DateTime LastPlayed { get; set; }

    public bool Playing { get; set; }
    
    public double CurrentTime { get; set; }

    public Guid? DeviceId { get; set; }

    public bool AutoPlay { get; set; }
}