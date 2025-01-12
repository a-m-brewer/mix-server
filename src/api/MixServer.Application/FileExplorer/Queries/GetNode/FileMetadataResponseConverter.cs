using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public interface IFileMetadataResponseConverter
    : IConverter<IFileMetadata, FileMetadataResponse>,
        IConverter<IMediaMetadata, MediaMetadataResponse>
{
}

public class FileMetadataResponseConverter : IFileMetadataResponseConverter
{
    public FileMetadataResponse Convert(IFileMetadata value)
    {
        return value switch
        {
            IMediaMetadata mediaMetadata => Convert(mediaMetadata),
            _ => new FileMetadataResponse
            {
                MimeType = value.MimeType,
            }
        };
    }

    public MediaMetadataResponse Convert(IMediaMetadata value)
    {
        return new MediaMetadataResponse
        {
            Duration = value.Duration,
            Bitrate = value.Bitrate,
            Tracklist = value.Tracklist
        };
    }
}