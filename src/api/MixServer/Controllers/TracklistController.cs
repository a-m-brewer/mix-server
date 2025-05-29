using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Tracklists.Commands.ImportTracklist;
using MixServer.Application.Tracklists.Commands.SaveTracklist;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class TracklistController(
    ICommandHandler<ImportTracklistCommand, ImportTracklistResponse> importTracklistCommandHandler,
    ICommandHandler<SaveTracklistCommand, SaveTracklistResponse> saveTracklistCommandHandler)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SaveTracklistResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveTracklist([FromBody] SaveTracklistCommand command, CancellationToken cancellationToken)
    {
        return Ok(await saveTracklistCommandHandler.HandleAsync(command, cancellationToken));
    }
    
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportTracklistResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportTracklist([FromForm] ImportTracklistCommand command, CancellationToken cancellationToken)
    {
        return Ok(await importTracklistCommandHandler.HandleAsync(command, cancellationToken));
    }
    
}