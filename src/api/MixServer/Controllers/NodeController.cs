using Microsoft.AspNetCore.Mvc;
using MixServer.Application.FileExplorer.Commands.SetFolderSort;
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class NodeController : ControllerBase
{
    private readonly IQueryHandler<GetFolderNodeQuery, FolderNodeResponse> _getFolderNodeQueryHandler;
    private readonly ICommandHandler<SetFolderSortCommand> _setFolderSortCommandHandler;

    public NodeController(IQueryHandler<GetFolderNodeQuery, FolderNodeResponse> getFolderNodeQueryHandler,
        ICommandHandler<SetFolderSortCommand> setFolderSortCommandHandler)
    {
        _getFolderNodeQueryHandler = getFolderNodeQueryHandler;
        _setFolderSortCommandHandler = setFolderSortCommandHandler;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(FolderNodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNode([FromQuery] GetFolderNodeQuery query)
    {
        return Ok(await _getFolderNodeQueryHandler.HandleAsync(query));
    }

    [HttpPost("sort")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetFolderSortMode([FromBody] SetFolderSortCommand command)
    {
        await _setFolderSortCommandHandler.HandleAsync(command);

        return NoContent();
    }
}