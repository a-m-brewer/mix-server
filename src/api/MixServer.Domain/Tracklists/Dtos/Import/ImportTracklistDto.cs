using Newtonsoft.Json;

namespace MixServer.Domain.Tracklists.Dtos.Import;

public class ImportTracklistDto
{
    [JsonProperty("cues", NullValueHandling = NullValueHandling.Ignore)]
    public List<ImportCueDto> Cues { get; set; } = [];
}