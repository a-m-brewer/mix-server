using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Streams.Commands;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class TranscodeController(
    ICommandHandler<RequestTranscodeCommand> requestTranscodeCommandHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RequestTranscode([FromBody] RequestTranscodeCommand command)
    {
        await requestTranscodeCommandHandler.HandleAsync(command);
        
        return Accepted();
    }
}