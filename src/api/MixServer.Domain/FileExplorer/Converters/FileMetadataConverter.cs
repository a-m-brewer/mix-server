using System.Text.RegularExpressions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileMetadataConverter : IConverter<FileInfo, IFileMetadata>
{
    FileMetadataEntity ConvertToEntity(FileInfo fileInfo, FileExplorerFileNodeEntity node);
}

public partial class FileMetadataConverter(
    IMimeTypeService mimeTypeService,
    IRootFileExplorerFolder rootFileExplorerFolder)
    : IFileMetadataConverter
{
    private static readonly HashSet<string> ExcludedMediaMimeTypes =
    [
        "video/vnd.dlna.mpeg-tts"
    ];
    
    public IFileMetadata Convert(FileInfo file)
    {
        var nodePath = rootFileExplorerFolder.GetNodePath(file.FullName);
        
        var mimeType = mimeTypeService.GetMimeType(nodePath);

        var isMedia = !string.IsNullOrWhiteSpace(mimeType) &&
                      AudioVideoMimeTypeRegex().IsMatch(mimeType) &&
                      !ExcludedMediaMimeTypes.Contains(mimeType);

        return new FileMetadata
        {
            MimeType = mimeType,
            IsMedia = isMedia
        };
    }

    public FileMetadataEntity ConvertToEntity(FileInfo fileInfo, FileExplorerFileNodeEntity node)
    {
        var metadata = Convert(fileInfo);

        return new FileMetadataEntity
        {
            Id = Guid.NewGuid(),
            MimeType = metadata.MimeType,
            IsMedia = metadata.IsMedia,
            Node = node,
            NodeId = node.Id
        };
    }

    [GeneratedRegex(@"^(audio|video)\/(.*)")]
    private static partial Regex AudioVideoMimeTypeRegex();
}