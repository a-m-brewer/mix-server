using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;

namespace MixServer.Domain.FileExplorer.Services.Caching;

public interface IMediaInfoCache
{
    bool TryGet(string absoluteFilePath, [MaybeNullWhen(false)] out MediaInfo mediaInfo);
    IReadOnlyCollection<NodePath> Remove(IEnumerable<string> absoluteFilePaths);
    void AddOrReplace(List<MediaInfo> mediaInfo);
}

public class MediaInfoCache : IMediaInfoCache
{
    private ConcurrentDictionary<string, MediaInfo> _cache = new();
    
    public bool TryGet(string absoluteFilePath, [MaybeNullWhen(false)] out MediaInfo mediaInfo)
    {
        return _cache.TryGetValue(absoluteFilePath, out mediaInfo);
    }

    public IReadOnlyCollection<NodePath> Remove(IEnumerable<string> absoluteFilePaths)
    {
        var nodePaths = new List<NodePath>();
        foreach (var absoluteFilePath in absoluteFilePaths)
        {
            if (_cache.TryRemove(absoluteFilePath, out var mediaInfo))
            {
                nodePaths.Add(mediaInfo.NodePath);
            }
        }
        
        return nodePaths;
    }

    public void AddOrReplace(List<MediaInfo> mediaInfo)
    {
        foreach (var info in mediaInfo)
        {
            _cache[info.NodePath.AbsolutePath] = info;
        }
    }
}