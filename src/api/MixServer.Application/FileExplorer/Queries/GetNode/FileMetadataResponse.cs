using System.Runtime.Serialization;
using JetBrains.Annotations;
using MixServer.Domain.Constants;
using MixServer.Domain.Tracklists.Dtos.Import;
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
    public string Duration { get; init; } = string.Empty;
    
    public int Bitrate { get; init; }

    public ImportTracklistDto Tracklist { get; init; } = new ();
}