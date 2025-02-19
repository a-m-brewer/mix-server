using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Streams.Queries;
using MixServer.Application.Streams.Queries.GetStream;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Models;

namespace MixServer.Controllers;

[Route("api/[controller]")]
[AllowAnonymous]
public class StreamController(IQueryHandler<GetStreamQuery, StreamFile> getStreamQueryHandler)
    : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetStream(
        // ReSharper disable once InconsistentNaming - so it matches signalR code
        [FromQuery] string? access_token,
        [FromRoute] string id)
    {
        var stream = await getStreamQueryHandler.HandleAsync(new GetStreamQuery
        {
            Id = id,
            AccessToken = access_token ?? string.Empty
        });
        
        return new PhysicalFileResult(stream.FilePath, stream.ContentType)
        {
            EnableRangeProcessing = true
        };
    }
}