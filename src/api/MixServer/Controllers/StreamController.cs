using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Streams.Queries;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[Route("api/[controller]")]
public class StreamController(IQueryHandler<GetStreamQuery, GetStreamQueryResponse> getStreamQueryHandler)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{playbackSessionId:guid}")]
    public async Task<IActionResult> GetStream(
        // ReSharper disable once InconsistentNaming - so it matches signalR code
        [FromQuery] string? access_token,
        [FromRoute] Guid playbackSessionId)
    {
        var stream = await getStreamQueryHandler.HandleAsync(new GetStreamQuery
        {
            PlaybackSessionId = playbackSessionId,
            AccessToken = access_token ?? throw new UnauthorizedRequestException()
        });
        
        return new PhysicalFileResult(stream.AbsoluteFilePath, stream.ContentType)
        {
            EnableRangeProcessing = true
        };
    }
}