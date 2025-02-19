using System.Text;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Caches;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public interface IFileExplorerResponseConverter
    : IConverter<IFileExplorerNode, FileExplorerNodeResponse>,
        IConverter<IFileExplorerFileNode, FileExplorerFileNodeResponse>,
        IConverter<IFileExplorerFolderNode, FileExplorerFolderNodeResponse>,
        IConverter<IFileExplorerFolder, FileExplorerFolderResponse>,
        IConverter<IRootFileExplorerFolder, RootFileExplorerFolderResponse>
{
}

public class FileExplorerResponseConverter(ITranscodeCache transcodeCache) : IFileExplorerResponseConverter
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

    private FileMetadataResponse Convert(IFileMetadata value, string absoluteFilePath)
    {
        return value switch
        {
            IMediaMetadata mediaMetadata => Convert(mediaMetadata, absoluteFilePath),
            _ => new FileMetadataResponse
            {
                MimeType = value.MimeType,
            }
        };
    }

    private MediaMetadataResponse Convert(IMediaMetadata value, string absoluteFilePath)
    {
        return new MediaMetadataResponse
        {
            MimeType = value.MimeType,
            Duration = FormatTimespan(value.Duration),
            Bitrate = value.Bitrate,
            TranscodeState = transcodeCache.GetTranscodeStatus(absoluteFilePath),
            Tracklist = value.Tracklist
        };
    }

    private static string FormatTimespan(TimeSpan duration)
    {
        var sb = new StringBuilder();

        var previousAdded = false;

        if (duration.Days > 0)
        {
            sb.Append($"{duration.Days:D1}.");
            previousAdded = true;
        }

        if (duration.Hours > 0 || previousAdded)
        {
            sb.Append($"{duration.Hours:D2}:");
            previousAdded = true;
        }

        if (duration.Minutes > 0 || previousAdded)
        {
            sb.Append($"{duration.Minutes:D2}:");
        }

        sb.Append($"{duration.Seconds:D2}");

        return sb.ToString();
    }
}