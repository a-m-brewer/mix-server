using Microsoft.AspNetCore.Mvc;
using MixServer.Application.FileExplorer.Commands.RefreshFolder;
using MixServer.Application.FileExplorer.Commands.SetFolderSort;
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class NodeController(
    IQueryHandler<GetFolderNodeQuery, FileExplorerFolderResponse> getFolderNodeQueryHandler,
    ICommandHandler<RefreshFolderCommand, FileExplorerFolderResponse> refreshFolderCommandHandler,
    ICommandHandler<SetFolderSortCommand> setFolderSortCommandHandler)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(FileExplorerFolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNode([FromQuery] GetFolderNodeQuery query, CancellationToken cancellationToken)
    {
        return Ok(await getFolderNodeQueryHandler.HandleAsync(query, cancellationToken));
    }
    
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(FileExplorerFolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshFolder([FromBody] RefreshFolderCommand command, CancellationToken cancellationToken)
    {
        return Ok(await refreshFolderCommandHandler.HandleAsync(command, cancellationToken));
    }

    [HttpPost("sort")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetFolderSortMode([FromBody] SetFolderSortCommand command, CancellationToken cancellationToken)
    {
        await setFolderSortCommandHandler.HandleAsync(command, cancellationToken);

        return NoContent();
    }
}