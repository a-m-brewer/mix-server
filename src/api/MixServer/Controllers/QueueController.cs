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
public class QueueController : ControllerBase
{
    private readonly ICommandHandler<AddToQueueCommand> _addToQueueCommandHandler;
    private readonly IQueryHandler<QueueSnapshotDto> _getCurrentQueueQueryHandler;
    private readonly ICommandHandler<RemoveFromQueueCommand> _removeFromQueueCommandHandler;
    private readonly ICommandHandler<SetQueuePositionCommand> _setQueuePositionCommandHandler;

    public QueueController(
        ICommandHandler<AddToQueueCommand> addToQueueCommandHandler,
        IQueryHandler<QueueSnapshotDto> getCurrentQueueQueryHandler,
        ICommandHandler<RemoveFromQueueCommand> removeFromQueueCommandHandler,
        ICommandHandler<SetQueuePositionCommand> setQueuePositionCommandHandler)
    {
        _addToQueueCommandHandler = addToQueueCommandHandler;
        _getCurrentQueueQueryHandler = getCurrentQueueQueryHandler;
        _removeFromQueueCommandHandler = removeFromQueueCommandHandler;
        _setQueuePositionCommandHandler = setQueuePositionCommandHandler;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(QueueSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Queue()
    {
        return Ok(await _getCurrentQueueQueryHandler.HandleAsync());
    }
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddToQueue([FromBody] AddToQueueCommand command)
    {
        await _addToQueueCommandHandler.HandleAsync(command);

        return NoContent();
    }

    [HttpDelete("item/{queueItemId:guid}")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromQueue([FromRoute] Guid queueItemId)
    {
        await _removeFromQueueCommandHandler.HandleAsync(new RemoveFromQueueCommand { QueueItems = [queueItemId] });

        return NoContent();
    }

    [HttpDelete("item")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromQueue([FromBody] RemoveFromQueueCommand command)
    {
        await _removeFromQueueCommandHandler.HandleAsync(command);

        return NoContent();
    }

    [HttpPost("position")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetQueuePosition([FromBody] SetQueuePositionCommand command)
    {
        await _setQueuePositionCommandHandler.HandleAsync(command);

        return NoContent();
    }
}