using System.Runtime.Serialization;
using JetBrains.Annotations;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Infrastructure.Files.Constants;
using Newtonsoft.Json;
using NJsonSchema.NewtonsoftJson.Converters;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

[KnownType(nameof(GetKnownTypes))]
[JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
public class FileExplorerNodeResponse
{
    public string Name { get; init; } = string.Empty;

    public string AbsolutePath { get; init; } = string.Empty;

    public FileExplorerNodeType Type { get; init; }

    public bool Exists { get; init; }
    
    public DateTime CreationTimeUtc { get; init; }
    
    [UsedImplicitly]
    public static IEnumerable<Type> GetKnownTypes()
    {
        return new[]
        {
            typeof(FileExplorerNodeResponse),
            typeof(FileExplorerFileNodeResponse),
            typeof(FileExplorerFolderNodeResponse),
        };
    }
}

public class FileExplorerFileNodeResponse : FileExplorerNodeResponse
{
    public required FileMetadataResponse Metadata { get; init; }

    public required bool PlaybackSupported { get; init; }

    public required FileExplorerFolderNodeResponse Parent { get; init; }
}

public class FileExplorerFolderNodeResponse : FileExplorerNodeResponse
{
    public bool BelongsToRoot { get; init; }

    public bool BelongsToRootChild { get; init; }
    
    public FileExplorerFolderNodeResponse? Parent { get; init; }
}

[KnownType(nameof(GetKnownTypes))]
[JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
public class FileExplorerFolderResponse
{
    public FileExplorerFolderNodeResponse Node { get; init; } = new ();

    public IReadOnlyCollection<FileExplorerNodeResponse> Children { get; init; } = Array.Empty<FileExplorerNodeResponse>();

    public FolderSortDto Sort { get; set; } = FolderSortDto.Default;
    
    [UsedImplicitly]
    public static IEnumerable<Type> GetKnownTypes()
    {
        return new[]
        {
            typeof(FileExplorerFolderResponse),
            typeof(RootFileExplorerFolderResponse)
        };
    }
}

public class RootFileExplorerFolderResponse : FileExplorerFolderResponse;