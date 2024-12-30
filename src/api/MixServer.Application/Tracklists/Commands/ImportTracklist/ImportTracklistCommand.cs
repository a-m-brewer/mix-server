using Microsoft.AspNetCore.Http;

namespace MixServer.Application.Tracklists.Commands.ImportTracklist;

public class ImportTracklistCommand
{
    public required IFormFile File { get; set; }
}