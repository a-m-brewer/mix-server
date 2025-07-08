using MixServer.Domain.Tracklists.Dtos.Import;

namespace MixServer.Application.FileExplorer.Dtos;

public class MediaInfoDto
{
    public required NodePathDto NodePath { get; init; }
    
    public required int Bitrate { get; init; }
    
    public required string Duration { get; init; }
}