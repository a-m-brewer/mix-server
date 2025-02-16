using System.Runtime.Serialization;
using JetBrains.Annotations;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Tracklists.Dtos.Import;
using MixServer.Infrastructure.Files.Constants;
using Newtonsoft.Json;
using NJsonSchema.NewtonsoftJson.Converters;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

[KnownType(nameof(GetKnownTypes))]
[JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
public class FileMetadataResponse
{
    public string MimeType { get; init; } = MimeTypeConstants.DefaultMimeType;
    
    [UsedImplicitly]
    public static IEnumerable<Type> GetKnownTypes()
    {
        return
        [
            typeof(FileMetadataResponse),
            typeof(MediaMetadataResponse)
        ];
    }
}

public class MediaMetadataResponse : FileMetadataResponse
{
    public required string Duration { get; init; }
    
    public required int Bitrate { get; init; }
    
    public required string FileHash { get; init; }
    
    public required TranscodeState TranscodeState { get; init; }

    public required ImportTracklistDto Tracklist { get; init; }
}