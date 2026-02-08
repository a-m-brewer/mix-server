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
    public required NodePathDto Path { get; init; }

    public required FileExplorerNodeType Type { get; init; }

    public required bool Exists { get; init; }
    
    public required DateTime CreationTimeUtc { get; init; }
    
    [UsedImplicitly]
    public static IEnumerable<Type> GetKnownTypes()
    {
        return
        [
            typeof(FileExplorerNodeResponse),
            typeof(FileExplorerFileNodeResponse),
            typeof(FileExplorerFolderNodeResponse)
        ];
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
    public required bool BelongsToRoot { get; init; }

    public required bool BelongsToRootChild { get; init; }
    
    public required FileExplorerFolderNodeResponse? Parent { get; init; }
}

[KnownType(nameof(GetKnownTypes))]
[JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
public class FileExplorerFolderResponse
{
    public required FileExplorerFolderNodeResponse Node { get; init; }

    public IReadOnlyCollection<FileExplorerNodeResponse> Children { get; set; } = [];

    public int TotalCount { get; set; }

    public FolderSortDto Sort { get; set; } = FolderSortDto.Default;
    
    [UsedImplicitly]
    public static IEnumerable<Type> GetKnownTypes()
    {
        return
        [
            typeof(FileExplorerFolderResponse),
            typeof(RootFileExplorerFolderResponse)
        ];
    }
}

public class RootFileExplorerFolderResponse : FileExplorerFolderResponse;