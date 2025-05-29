using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Tracklists.Dtos.Import;
using Newtonsoft.Json;

namespace MixServer.Application.Tracklists.Commands.ImportTracklist;

public class ImportTracklistCommandHandler : ICommandHandler<ImportTracklistCommand, ImportTracklistResponse>
{
    public async Task<ImportTracklistResponse> HandleAsync(ImportTracklistCommand request, CancellationToken cancellationToken = default)
    {
        await using var stream = request.File.OpenReadStream();
        using var reader = new StreamReader(stream);
        await using var jsonReader = new JsonTextReader(reader);

        var serializer = new JsonSerializer();
        
        var tracklistDto = serializer.Deserialize<ImportTracklistDto>(jsonReader);

        if (tracklistDto is null)
        {
            throw new InvalidRequestException(nameof(request.File), "Failed to deserialize the tracklist file.");
        }
        
        var sanitizedTracklist = SanitizeTracklist(tracklistDto);
        
        return new ImportTracklistResponse(sanitizedTracklist);
    }

    private static ImportTracklistDto SanitizeTracklist(ImportTracklistDto tracklistDto)
    {
        foreach (var track in tracklistDto.Cues.SelectMany(cue => cue.Tracks))
        {
            track.Name = track.Name.Trim().Replace('/', '-');
            track.Artist = track.Artist.Trim().Replace('/', '-');
        }

        return new ImportTracklistDto
        {
            Cues = tracklistDto.Cues
                .GroupBy(g => g.Cue)
                .Select(s => new ImportCueDto
                {
                    Cue = s.Key,
                    Tracks = s.SelectMany(t => t.Tracks).ToList()
                })
                .ToList()
        };
    }
}