using Newtonsoft.Json;

namespace MixServer.Domain.Tracklists.Dtos.Import;

public class ImportTrackDto
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("artist", NullValueHandling = NullValueHandling.Ignore)]
    public string Artist { get; set; } = string.Empty;

    [JsonProperty("players", NullValueHandling = NullValueHandling.Ignore)]
    public List<ImportPlayerDto> Players { get; set; } = [];
}