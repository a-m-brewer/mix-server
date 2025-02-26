using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Streams.Commands.GenerateStreamKey;
using MixServer.Application.Streams.Queries;
using MixServer.Application.Streams.Queries.GetStream;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Models;

namespace MixServer.Controllers;

[Route("api/[controller]")]
public class StreamController(IQueryHandler<GetStreamQuery, StreamFile> getStreamQueryHandler,
    ICommandHandler<GenerateStreamKeyCommand, GenerateStreamKeyResponse> generateStreamKeyCommandHandler)
    : ControllerBase
{
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStream([FromRoute] string id, [FromQuery] StreamSecurityParametersDto securityParameters)
    {
        var stream = await getStreamQueryHandler.HandleAsync(new GetStreamQuery
        {
            Id = id,
            SecurityParameters = securityParameters
        });
        
        return new PhysicalFileResult(stream.FilePath, stream.ContentType)
        {
            EnableRangeProcessing = true
        };
    }
    
    [HttpGet("key/{playbackSessionId:guid}")]
    [ProducesResponseType(typeof(GenerateStreamKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateStreamKey([FromRoute] Guid playbackSessionId)
    {
        var response = await generateStreamKeyCommandHandler.HandleAsync(new GenerateStreamKeyCommand
        {
            PlaybackSessionId = playbackSessionId
        });
        
        return Ok(response);
    }
}