using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Streams.Queries.GetSegment;
using MixServer.Application.Streams.Queries.GetStream;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Models;

namespace MixServer.Controllers;

[Route("api/[controller]")]
public class StreamController(IQueryHandler<GetStreamQuery, HttpFileInfo> getStreamQueryHandler,
    IQueryHandler<GetSegmentQuery, SegmentFileInfo> getSegmentQueryHandler)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetStream(
        // ReSharper disable once InconsistentNaming - so it matches signalR code
        [FromQuery] string? access_token,
        [FromRoute] string id,
        [FromQuery] bool transcode = false)
    {
        if (Guid.TryParse(id, out var playbackSessionId))
        {
            var stream = await getStreamQueryHandler.HandleAsync(new GetStreamQuery
            {
                PlaybackSessionId = playbackSessionId,
                AccessToken = access_token ?? throw new UnauthorizedRequestException(),
                Transcode = transcode
            });
        
            return new PhysicalFileResult(stream.Path, stream.MimeType)
            {
                EnableRangeProcessing = true
            };
        }

        if (id.EndsWith(".ts"))
        {
            var segment = await getSegmentQueryHandler.HandleAsync(new GetSegmentQuery
            {
                Segment = id
            });
            
            return new PhysicalFileResult(segment.Path, segment.MimeType)
            {
                EnableRangeProcessing = true
            };
        }

        return BadRequest();
    }
}