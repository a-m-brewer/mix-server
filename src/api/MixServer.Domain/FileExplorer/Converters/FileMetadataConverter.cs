using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Domain.Tracklists.Services;
using MixServer.Domain.Utilities;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileMetadataConverter : IConverter<FileInfo, IFileMetadata>;

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
    
    [GeneratedRegex(@"^(audio|video)\/(.*)")]
    private static partial Regex AudioVideoMimeTypeRegex();
}