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
public abstract class NodeResponse
{
    protected NodeResponse(
        string name,
        string nameIdentifier,
        string? absolutePath,
        FileExplorerNodeType type,
        bool exists)
    {
        Name = name;
        NameIdentifier = nameIdentifier;
        AbsolutePath = absolutePath;
        Type = type;
        Exists = exists;
    }
    
    public string Name { get; }
    
    public string NameIdentifier { get; }
    
    public string? AbsolutePath { get; }

    public FileExplorerNodeType Type { get; }
    
    public bool Exists { get; }
    
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

public class FileNodeResponse : NodeResponse
{
    public FileNodeResponse(
        string name,
        string nameIdentifier,
        string? absolutePath,
        FileExplorerNodeType type,
        bool exists,
        string? mimeType,
        bool playbackSupported,
        FolderInfoResponse parent) : base(name, nameIdentifier, absolutePath, type, exists)
    {
        MimeType = mimeType;
        PlaybackSupported = playbackSupported;
        Parent = parent;
    }
    
    public string? MimeType { get; }

    public bool PlaybackSupported { get; }
    
    public FolderInfoResponse Parent { get; }
}

public class FolderNodeResponse : NodeResponse
{
    public FolderNodeResponse(
        string name,
        string nameIdentifier,
        string? absolutePath,
        FileExplorerNodeType type,
        bool exists,
        string? parentAbsolutePath,
        FolderSortDto sort) 
        : base(name, nameIdentifier, absolutePath, type, exists)
    {
        ParentAbsolutePath = parentAbsolutePath;
        Sort = sort;
    }

    public string? ParentAbsolutePath { get; set; }
    public List<NodeResponse> Children { get; set; } = [];

    public FolderSortDto Sort { get; set; }
}

public class RootFolderNodeResponse : FolderNodeResponse
{
    public RootFolderNodeResponse(
        string name,
        string nameIdentifier,
        string? absolutePath,
        FileExplorerNodeType type,
        bool exists) 
        : base(name, nameIdentifier, absolutePath, type, exists, null, FolderSortDto.Default)
    {
    }
}

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