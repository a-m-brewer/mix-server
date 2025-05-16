using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Domain.Tracklists.Services;
using MixServer.Domain.Utilities;
using MixServer.Shared.Interfaces;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileMetadataConverter : IConverter<FileInfo, IFileMetadata>;

public partial class FileMetadataConverter(
    ILogger<FileMetadataConverter> logger,
    ITagBuilderFactory tagBuilderFactory,
    ITracklistTagService tracklistTagService,
    IMimeTypeService mimeTypeService)
    : IFileMetadataConverter
{
    private static readonly HashSet<string> ExcludedMediaMimeTypes =
    [
        "video/vnd.dlna.mpeg-tts"
    ];
    
    public IFileMetadata Convert(FileInfo file)
    {
        var mimeType = mimeTypeService.GetMimeType(file.FullName, file.Extension);

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