using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Queueing.Commands.AddToQueue;
using MixServer.Application.Queueing.Commands.RemoveFromQueue;
using MixServer.Application.Queueing.Commands.SetQueuePosition;
using MixServer.Application.Queueing.Queries.GetCurrentQueue;
using MixServer.Application.Queueing.Responses;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class QueueController(
    ICommandHandler<AddToQueueCommand, QueuePositionDto> addToQueueCommandHandler,
    IQueryHandler<GetCurrentQueueRequest, QueueRangeDto> getCurrentQueueQueryHandler,
    ICommandHandler<RemoveFromQueueCommand, QueuePositionDto> removeFromQueueCommandHandler,
    IQueryHandler<QueuePositionDto> getQueuePositionQueryHandler,
    ICommandHandler<SetQueuePositionCommand, CurrentSessionUpdatedDto> setQueuePositionCommandHandler)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(QueueRangeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Queue([FromQuery] GetCurrentQueueRequest request, CancellationToken cancellationToken)
    {
        return Ok(await getCurrentQueueQueryHandler.HandleAsync(request, cancellationToken));
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(QueuePositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddToQueue([FromBody] AddToQueueCommand command, CancellationToken cancellationToken)
    {
        return Ok(await addToQueueCommandHandler.HandleAsync(command, cancellationToken));
    }

    [HttpDelete("item/{queueItemId:guid}")]
    [ProducesResponseType(typeof(QueuePositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromQueue([FromRoute] Guid queueItemId, CancellationToken cancellationToken)
    {
        return Ok(await removeFromQueueCommandHandler.HandleAsync(new RemoveFromQueueCommand { QueueItems = [queueItemId] }, cancellationToken));
    }

    [HttpDelete("item")]
    [ProducesResponseType(typeof(QueuePositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromQueue([FromBody] RemoveFromQueueCommand command, CancellationToken cancellationToken)
    {
        return Ok(await removeFromQueueCommandHandler.HandleAsync(command, cancellationToken));
    }

    [HttpGet("position")]
    [ProducesResponseType(typeof(QueuePositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetQueuePosition(CancellationToken cancellationToken)
    {
        return Ok(await getQueuePositionQueryHandler.HandleAsync(cancellationToken));
    }

    [HttpPost("position")]
    [ProducesResponseType(typeof(CurrentSessionUpdatedDto),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetQueuePosition([FromBody] SetQueuePositionCommand command, CancellationToken cancellationToken)
    {
        return Ok(await setQueuePositionCommandHandler.HandleAsync(command, cancellationToken));
    }
}