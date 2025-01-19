using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixServer.Application.FileExplorer.Commands.CopyNode;
using MixServer.Application.FileExplorer.Commands.DeleteNode;
using MixServer.Auth;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
[Authorize(Policy = Policies.IsAdminOrOwner)]
public class NodeManagementController(
    ICommandHandler<CopyNodeCommand> copyNodeCommandHandler,
    ICommandHandler<DeleteNodeCommand> deleteNodeCommandHandler)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CopyNode([FromBody] CopyNodeCommand command)
    {
        await copyNodeCommandHandler.HandleAsync(command);

        return NoContent();
    }
    
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNode([FromBody] DeleteNodeCommand command)
    {
        await deleteNodeCommandHandler.HandleAsync(command);

        return NoContent();
    }
}