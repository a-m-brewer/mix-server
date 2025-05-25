using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;

namespace MixServer.Domain.FileExplorer.Services.Caching;

public interface IMediaInfoCache
{
    bool TryGet(NodePath path, [MaybeNullWhen(false)] out MediaInfo mediaInfo);
    IReadOnlyCollection<NodePath> Remove(IEnumerable<NodePath> nodePaths);
    void AddOrReplace(List<MediaInfo> mediaInfo);
}

public class MediaInfoCache : IMediaInfoCache
{
    private ConcurrentDictionary<NodePath, MediaInfo> _cache = new();
    
    public bool TryGet(NodePath absoluteFilePath, [MaybeNullWhen(false)] out MediaInfo mediaInfo)
    {
        return _cache.TryGetValue(absoluteFilePath, out mediaInfo);
    }

    public IReadOnlyCollection<NodePath> Remove(IEnumerable<NodePath> nodePaths)
    {
        var outputNodePaths = new List<NodePath>();
        foreach (var nodePath in nodePaths)
        {
            if (_cache.TryRemove(nodePath, out var mediaInfo))
            {
                outputNodePaths.Add(mediaInfo.Path);
            }
        }
        
        return outputNodePaths;
    }

    public void AddOrReplace(List<MediaInfo> mediaInfo)
    {
        foreach (var info in mediaInfo)
        {
            _cache[info.Path] = info;
        }
    }
}