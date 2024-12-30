using MixServer.Domain.Tracklists.Enums;
using Newtonsoft.Json;

namespace MixServer.Domain.Tracklists.Dtos.Import;

public class ImportPlayerDto
{
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public TracklistPlayerType Type { get; set; }

    [JsonProperty("urls", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Urls { get; set; } = [];
}