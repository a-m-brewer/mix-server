using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Tracklists.Dtos.Import;
using Newtonsoft.Json;

namespace MixServer.Application.Tracklists.Commands.ImportTracklist;

public class ImportTracklistCommandHandler : ICommandHandler<ImportTracklistCommand, ImportTracklistResponse>
{
    public async Task<ImportTracklistResponse> HandleAsync(ImportTracklistCommand request)
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
        
        return new ImportTracklistResponse(tracklistDto);
    }
}