using Newtonsoft.Json;

namespace MixServer.Domain.Tracklists.Dtos.Import;

public class ImportCueDto
{
    [JsonProperty("cue", NullValueHandling = NullValueHandling.Ignore)]
    public TimeSpan Cue { get; set; }

    [JsonProperty("tracks", NullValueHandling = NullValueHandling.Ignore)]
    public List<ImportTrackDto> Tracks { get; set; } = [];
}