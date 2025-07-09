using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Tracklists.Dtos.Import;
using MixServer.Domain.Tracklists.Entities;

namespace MixServer.Domain.Tracklists.Converters;

public interface ITracklistDtoConverter :
    IConverter<TracklistEntity?, ImportTracklistDto>,
    IConverter<CueEntity, ImportCueDto>,
    IConverter<TrackEntity, ImportTrackDto>,
    IConverter<List<TracklistPlayersEntity>, List<ImportPlayerDto>>;

public interface ITracklistEntityConverter :
    IConverter<ImportTracklistDto, FileExplorerFileNodeEntity, TracklistEntity>,
    IConverter<ImportCueDto, TracklistEntity, CueEntity>,
    IConverter<ImportTrackDto, CueEntity, TrackEntity>,
    IConverter<ImportPlayerDto, TrackEntity, List<TracklistPlayersEntity>>,
    IConverter<ImportPlayerDto, TrackEntity, string, TracklistPlayersEntity> ;

public interface ITracklistConverter
    : ITracklistDtoConverter, ITracklistEntityConverter;

public class TracklistConverter : ITracklistConverter
{
    public ImportTracklistDto Convert(TracklistEntity? value)
    {
        if (value is null)
        {
            return new ImportTracklistDto();
        }

        return new ImportTracklistDto
        {
            Cues = value.Cues.Select(Convert).ToList()
        };
    }

    public ImportCueDto Convert(CueEntity value)
    {
        return new ImportCueDto
        {
            Cue = value.Cue,
            Tracks = value.Tracks.Select(Convert).ToList()
        };
    }

    public ImportTrackDto Convert(TrackEntity value)
    {
        return new ImportTrackDto
        {
            Artist = value.Artist,
            Name = value.Name,
            Players = Convert(value.Players)
        };
    }

    public List<ImportPlayerDto> Convert(List<TracklistPlayersEntity> value)
    {
        return value.GroupBy(g => g.Type)
            .Select(s => new ImportPlayerDto
            {
                Type = s.Key,
                Urls = s.Select(p => p.Url).ToList()
            })
            .ToList();
    }

    public TracklistEntity  Convert(ImportTracklistDto tracklist, FileExplorerFileNodeEntity file)
    {
        var entity = new TracklistEntity
        {
            Id = Guid.NewGuid(),
            Node = file,
            NodeId = file.Id
        };

        entity.Cues = tracklist.Cues.Select(c => Convert(c, entity)).ToList();
        
        return entity;
    }

    public CueEntity Convert(ImportCueDto value, TracklistEntity tracklist)
    {
        var entity = new CueEntity
        {
            Id = Guid.NewGuid(),
            Cue = value.Cue,
            Tracklist = tracklist,
            TracklistId = tracklist.Id
        };
        
        entity.Tracks = value.Tracks.Select(t => Convert(t, entity)).ToList();
        
        return entity;
    }

    public TrackEntity Convert(ImportTrackDto value, CueEntity cue)
    {
        var entity = new TrackEntity
        {
            Id = Guid.NewGuid(),
            Name = value.Name,
            Artist = value.Artist,
            Cue = cue
        };
        
        entity.Players = value.Players.Select(p => Convert(p, entity)).SelectMany(s => s).ToList();
        
        return entity;
    }

    public List<TracklistPlayersEntity> Convert(ImportPlayerDto value, TrackEntity track)
    {
        return value.Urls.Select(s => Convert(value, track, s)).ToList();
    }

    public TracklistPlayersEntity Convert(ImportPlayerDto value, TrackEntity track, string url)
    {
        return new TracklistPlayersEntity
        {
            Id = Guid.NewGuid(),
            Type = value.Type,
            Url = url,
            Track = track
        };
    }
}