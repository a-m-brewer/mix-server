using Microsoft.AspNetCore.Mvc;
using MixServer.Application.FileExplorer.Commands.RefreshFolder;
using MixServer.Application.FileExplorer.Commands.SetFolderSort;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class NodeController(
    IQueryHandler<RangedNodePathRequestDto, RangedFileExplorerFolderResponse> getFolderNodeQueryHandler,
    IQueryHandler<FolderScanStatusDto> getFolderScanStatusQueryHandler,
    ICommandHandler<RefreshFolderCommand> refreshFolderCommandHandler,
    ICommandHandler<SetFolderSortCommand> setFolderSortCommandHandler)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(RangedFileExplorerFolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNode([FromQuery] RangedNodePathRequestDto query, CancellationToken cancellationToken)
    {
        return Ok(await getFolderNodeQueryHandler.HandleAsync(query, cancellationToken));
    }
    
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshFolder([FromBody] RefreshFolderCommand command, CancellationToken cancellationToken)
    {
        await refreshFolderCommandHandler.HandleAsync(command, cancellationToken);
        
        return NoContent();
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
    
    [HttpGet("scan")]
    [ProducesResponseType(typeof(FolderScanStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFolderScanStatus(CancellationToken cancellationToken)
    {
        return Ok(await getFolderScanStatusQueryHandler.HandleAsync(cancellationToken));
    }
}