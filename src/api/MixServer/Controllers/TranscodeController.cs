using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Streams.Commands;
using MixServer.Application.Streams.Commands.RequestTranscode;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class TranscodeController(
    ICommandHandler<RequestTranscodeCommand> requestTranscodeCommandHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestTranscode([FromBody] RequestTranscodeCommand command, CancellationToken cancellationToken)
    {
        await requestTranscodeCommandHandler.HandleAsync(command, cancellationToken);
        
        return Accepted();
    }
}