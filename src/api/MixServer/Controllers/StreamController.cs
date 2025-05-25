using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Streams.Queries.GetStream;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Models;

namespace MixServer.Controllers;

[Route("api/[controller]")]
public class StreamController(IQueryHandler<GetStreamQuery, StreamFile> getStreamQueryHandler)
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
        
        return new PhysicalFileResult(stream.FilePath.AbsolutePath, stream.ContentType)
        {
            EnableRangeProcessing = true
        };
    }
}