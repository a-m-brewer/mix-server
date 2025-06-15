using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Tracklists.Converters;
using MixServer.Domain.Tracklists.Entities;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IMediaMetadataEntityConverter : IConverter<NodePath, MediaMetadataEntity, TracklistEntity?, MediaInfo>;

public class MediaMetadataEntityConverter(ITracklistConverter tracklistConverter) : IMediaMetadataEntityConverter
{
    public MediaInfo Convert(NodePath path, MediaMetadataEntity metadata, TracklistEntity? tracklist)
    {
        return new MediaInfo
        {
            Path = path,
            Bitrate = metadata.Bitrate,
            Duration = metadata.Duration,
            Tracklist = tracklistConverter.Convert(tracklist)
        };
    }
}