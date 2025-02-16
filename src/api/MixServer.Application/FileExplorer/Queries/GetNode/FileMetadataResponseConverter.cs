using System.Text;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Caches;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public interface IFileMetadataResponseConverter
    : IConverter<IFileMetadata, FileMetadataResponse>,
        IConverter<IMediaMetadata, MediaMetadataResponse>
{
}

public class FileMetadataResponseConverter(ITranscodeCache transcodeCache) : IFileMetadataResponseConverter
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
            MimeType = value.MimeType,
            Duration = FormatTimespan(value.Duration),
            Bitrate = value.Bitrate,
            FileHash = value.FileHash,
            TranscodeState = transcodeCache.GetTranscodeStatus(value.FileHash),
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