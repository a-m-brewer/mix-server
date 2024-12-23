using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Tracklists.Commands;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class TracklistController(
    ICommandHandler<ImportTracklistCommand, ImportTracklistResponse> importTracklistCommandHandler)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ImportTracklistResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportTracklist([FromForm] ImportTracklistCommand command)
    {
        return Ok(await importTracklistCommandHandler.HandleAsync(command));
    }
    
}