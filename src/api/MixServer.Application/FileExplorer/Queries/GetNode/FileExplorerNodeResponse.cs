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

public class FileExplorerFolderInfoNodeResponse
{
    public string Name { get; init; } = string.Empty;

    public string AbsolutePath { get; init; } = string.Empty;

    public FileExplorerNodeType Type { get; init; }

    public bool Exists { get; init; }
    
    public DateTime CreationTimeUtc { get; init; }
    
    public bool BelongsToRoot { get; init; }

    public bool BelongsToRootChild { get; init; }
}

public class FileExplorerFileNodeResponse : FileExplorerNodeResponse
{
    public string MimeType { get; init; } = MimeTypeConstants.DefaultMimeType;

    public bool PlaybackSupported { get; init; }

    public FileExplorerFolderInfoNodeResponse Parent { get; init; } = new ();
}

public class FileExplorerFolderNodeResponse : FileExplorerNodeResponse
{
    public bool BelongsToRoot { get; init; }

    public bool BelongsToRootChild { get; init; }
    
    public FileExplorerFolderInfoNodeResponse? Parent { get; init; }
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