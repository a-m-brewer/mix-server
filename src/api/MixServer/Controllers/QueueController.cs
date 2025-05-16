using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Queueing.Commands.AddToQueue;
using MixServer.Application.Queueing.Commands.RemoveFromQueue;
using MixServer.Application.Queueing.Commands.SetQueuePosition;
using MixServer.Application.Queueing.Responses;
using MixServer.Application.Sessions.Dtos;
using MixServer.Shared.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class QueueController(
    ICommandHandler<AddToQueueCommand, QueueSnapshotDto> addToQueueCommandHandler,
    IQueryHandler<QueueSnapshotDto> getCurrentQueueQueryHandler,
    ICommandHandler<RemoveFromQueueCommand, QueueSnapshotDto> removeFromQueueCommandHandler,
    ICommandHandler<SetQueuePositionCommand, CurrentSessionUpdatedDto> setQueuePositionCommandHandler)
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
    [ProducesResponseType(typeof(QueueSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddToQueue([FromBody] AddToQueueCommand command)
    {
        return Ok(await addToQueueCommandHandler.HandleAsync(command));
    }

    [HttpDelete("item/{queueItemId:guid}")]
    [ProducesResponseType(typeof(QueueSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromQueue([FromRoute] Guid queueItemId)
    {
        return Ok(await removeFromQueueCommandHandler.HandleAsync(new RemoveFromQueueCommand { QueueItems = [queueItemId] }));
    }

    [HttpDelete("item")]
    [ProducesResponseType(typeof(QueueSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromQueue([FromBody] RemoveFromQueueCommand command)
    {
        return Ok(await removeFromQueueCommandHandler.HandleAsync(command));
    }

    [HttpPost("position")]
    [ProducesResponseType(typeof(CurrentSessionUpdatedDto),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetQueuePosition([FromBody] SetQueuePositionCommand command)
    {
        return Ok(await setQueuePositionCommandHandler.HandleAsync(command));
    }
}