using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Queueing.Commands.AddToQueue;
using MixServer.Application.Queueing.Commands.RemoveFromQueue;
using MixServer.Application.Queueing.Commands.SetQueuePosition;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class QueueController(
    ICommandHandler<AddToQueueCommand> addToQueueCommandHandler,
    IQueryHandler<QueueSnapshotDto> getCurrentQueueQueryHandler,
    ICommandHandler<RemoveFromQueueCommand> removeFromQueueCommandHandler,
    ICommandHandler<SetQueuePositionCommand> setQueuePositionCommandHandler)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(QueueSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Queue()
    {
        return Ok(await getCurrentQueueQueryHandler.HandleAsync());
    }
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddToQueue([FromBody] AddToQueueCommand command)
    {
        await addToQueueCommandHandler.HandleAsync(command);

        return NoContent();
    }

    [HttpDelete("item/{queueItemId:guid}")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromQueue([FromRoute] Guid queueItemId)
    {
        await removeFromQueueCommandHandler.HandleAsync(new RemoveFromQueueCommand { QueueItems = [queueItemId] });

        return NoContent();
    }

    [HttpDelete("item")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromQueue([FromBody] RemoveFromQueueCommand command)
    {
        await removeFromQueueCommandHandler.HandleAsync(command);

        return NoContent();
    }

    [HttpPost("position")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetQueuePosition([FromBody] SetQueuePositionCommand command)
    {
        await setQueuePositionCommandHandler.HandleAsync(command);

        return NoContent();
    }
}