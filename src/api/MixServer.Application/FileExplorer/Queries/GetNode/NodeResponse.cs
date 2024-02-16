using System.Runtime.Serialization;
using JetBrains.Annotations;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Enums;
using Newtonsoft.Json;
using NJsonSchema.NewtonsoftJson.Converters;


namespace MixServer.Application.FileExplorer.Queries.GetNode;

[KnownType(nameof(GetKnownTypes))]
[JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
public abstract class NodeResponse(
    string name,
    string nameIdentifier,
    string? absolutePath,
    FileExplorerNodeType type,
    bool exists)
{
    public string Name { get; } = name;

    public string NameIdentifier { get; } = nameIdentifier;

    public string? AbsolutePath { get; } = absolutePath;

    public FileExplorerNodeType Type { get; } = type;

    public bool Exists { get; } = exists;

    /// <summary>
    /// Used to return the known derived types, for serialization purposes.
    /// </summary>
    [UsedImplicitly]
    public static IEnumerable<Type> GetKnownTypes()
    {
        return new[]
        {
            typeof(NodeResponse),
            typeof(FolderNodeResponse),
            typeof(RootFolderNodeResponse),
            typeof(RootFolderChildNodeResponse),
            typeof(FileNodeResponse)
        };
    }
}

public class FileNodeResponse(
    string name,
    string nameIdentifier,
    string? absolutePath,
    FileExplorerNodeType type,
    bool exists,
    string? mimeType,
    bool playbackSupported,
    FolderInfoResponse parent)
    : NodeResponse(name, nameIdentifier, absolutePath, type, exists)
{
    public string? MimeType { get; } = mimeType;

    public bool PlaybackSupported { get; } = playbackSupported;

    public FolderInfoResponse Parent { get; } = parent;
}

public class FolderNodeResponse(
    string nameIdentifier,
    FolderInfoResponse info,
    FolderSortDto sort)
    : NodeResponse(info.Name, nameIdentifier, info.AbsolutePath, FileExplorerNodeType.Folder, info.Exists)
{
    public List<NodeResponse> Children { get; init; } = [];

    public FolderInfoResponse Info { get; } = info;

    public FolderSortDto Sort { get; init; } = sort;
}

public class RootFolderNodeResponse(
    string nameIdentifier,
    FolderInfoResponse info)
    : FolderNodeResponse(nameIdentifier, info, FolderSortDto.Default);

public class RootFolderChildNodeResponse(
    string nameIdentifier,
    FolderInfoResponse info,
    FolderSortDto sort)
    : FolderNodeResponse(nameIdentifier, info, sort);

public class FolderInfoResponse(
    string name,
    string? absolutePath,
    string? parentAbsolutePath,
    bool exists,
    bool belongsToRoot,
    bool belongsToRootChild)
{
    public string Name { get; } = name;
    public string? AbsolutePath { get; } = absolutePath;
    public string? ParentAbsolutePath { get; } = parentAbsolutePath;
    public bool Exists { get; } = exists;
    public bool BelongsToRoot { get; } = belongsToRoot;
    public bool BelongsToRootChild { get; } = belongsToRootChild;
}