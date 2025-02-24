using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.Streams.Enums;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class FileMetadataResponse
{
    public required string MimeType { get; init; }
    
    public required bool IsMedia { get; init; }
    
    public required MediaInfoDto? MediaInfo { get; init; }
    
    public required TranscodeState TranscodeStatus { get; init; }
}
