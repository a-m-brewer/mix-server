using System.Runtime.Serialization;
using JetBrains.Annotations;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
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
    string name,
    string nameIdentifier,
    string? absolutePath,
    FileExplorerNodeType type,
    bool exists,
    string? parentAbsolutePath,
    FolderSortDto sort)
    : NodeResponse(name, nameIdentifier, absolutePath, type, exists)
{
    public string? ParentAbsolutePath { get; set; } = parentAbsolutePath;
    public List<NodeResponse> Children { get; set; } = [];

    public FolderSortDto Sort { get; set; } = sort;
}

public class RootFolderNodeResponse(
    string name,
    string nameIdentifier,
    string? absolutePath,
    FileExplorerNodeType type,
    bool exists)
    : FolderNodeResponse(name, nameIdentifier, absolutePath, type, exists, null, FolderSortDto.Default);

public class FolderInfoResponse(
    string name,
    string? absolutePath,
    string? parentAbsolutePath,
    bool exists,
    bool canRead)
{
    public string Name { get; } = name;
    public string? AbsolutePath { get; } = absolutePath;
    public string? ParentAbsolutePath { get; } = parentAbsolutePath;
    public bool Exists { get; } = exists;
    public bool CanRead { get; } = canRead;
}