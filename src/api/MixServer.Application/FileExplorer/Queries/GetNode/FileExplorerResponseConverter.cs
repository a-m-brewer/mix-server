using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Enums;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public interface IFileExplorerResponseConverter
    : IConverter<IFileExplorerNode, FileExplorerNodeResponse>,
        IConverter<IFileExplorerFileNode, FileExplorerFileNodeResponse>,
        IConverter<IFileExplorerFolderNode, FileExplorerFolderNodeResponse>,
        IConverter<IFileExplorerFolder, FileExplorerFolderResponse>,
        IConverter<IRootFileExplorerFolder, RootFileExplorerFolderResponse>
{
}

public class FileExplorerResponseConverter(
    IMediaInfoCache mediaInfoCache,
    IMediaInfoDtoConverter mediaInfoDtoConverter,
    ITranscodeCache transcodeCache) : IFileExplorerResponseConverter
{
    public FileExplorerNodeResponse Convert(IFileExplorerNode value)
    {
        return value switch
        {
            IFileExplorerFileNode fileNode => Convert(fileNode),
            IFileExplorerFolderNode folderNode => Convert(folderNode),
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    public FileExplorerFileNodeResponse Convert(IFileExplorerFileNode value)
    {
        return new FileExplorerFileNodeResponse
        {
            AbsolutePath = value.AbsolutePath,
            Exists = value.Exists,
            Name = value.Name,
            Type = value.Type,
            CreationTimeUtc = value.CreationTimeUtc,
            Metadata = Convert(value.Metadata, value.AbsolutePath),
            PlaybackSupported = value.PlaybackSupported,
            Parent = Convert(value.Parent)
        };
    }

    public FileExplorerFolderNodeResponse Convert(IFileExplorerFolderNode value)
    {
        return new FileExplorerFolderNodeResponse
        {
            AbsolutePath = value.AbsolutePath,
            Exists = value.Exists,
            Name = value.Name,
            Type = value.Type,
            CreationTimeUtc = value.CreationTimeUtc,
            BelongsToRoot = value.BelongsToRoot,
            BelongsToRootChild = value.BelongsToRootChild,
            Parent = value.Parent is null ? null : Convert(value.Parent)
        };
    }

    public FileExplorerFolderResponse Convert(IFileExplorerFolder value)
    {
        return value switch
        {
            IRootFileExplorerFolder rootFolder => Convert(rootFolder),
            _ => new FileExplorerFolderResponse
            {
                Node = Convert(value.Node),
                Children = value.Children.Select(Convert).ToList(),
                Sort = new FolderSortDto(value.Sort)
            }
        };
    }

    public RootFileExplorerFolderResponse Convert(IRootFileExplorerFolder value)
    {
        return new RootFileExplorerFolderResponse
        {
            Node = Convert(value.Node),
            Children = value.Children.Select(Convert).ToList(),
            Sort = new FolderSortDto(value.Sort)
        };
    }

    private FileMetadataResponse Convert(IFileMetadata value, string absolutePath)
    {
        return new FileMetadataResponse
        {
            MediaInfo = value.IsMedia && mediaInfoCache.TryGet(absolutePath, out var mediaInfo)
                ? mediaInfoDtoConverter.Convert(mediaInfo)
                : null,
            IsMedia = value.IsMedia,
            MimeType = value.MimeType,
            TranscodeStatus = value.IsMedia
                ? transcodeCache.GetTranscodeStatus(absolutePath)
                : TranscodeState.None
        };
    }
}