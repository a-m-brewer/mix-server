using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Sessions.Commands.ClearCurrentSession;
using MixServer.Application.Sessions.Commands.RequestPause;
using MixServer.Application.Sessions.Commands.RequestPlayback;
using MixServer.Application.Sessions.Commands.SeekCommand;
using MixServer.Application.Sessions.Commands.SetCurrentSession;
using MixServer.Application.Sessions.Commands.SetNextSession;
using MixServer.Application.Sessions.Commands.SetPlaying;
using MixServer.Application.Sessions.Commands.SyncPlaybackSession;
using MixServer.Application.Sessions.Dtos;
using MixServer.Application.Sessions.Queries.GetUsersSessions;
using MixServer.Domain.Interfaces;
using MixServer.Requests;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class SessionController(
    ICommandHandler<ClearCurrentSessionCommand, CurrentSessionUpdatedDto> clearCurrentSessionCommandHandler,
    IQueryHandler<GetUsersSessionsQuery, GetUsersSessionsResponse> getUsersSessionsQueryHandler,
    ICommandHandler<RequestPauseCommand> requestPauseCommandHandler,
    ICommandHandler<RequestPlaybackCommand> requestPlaybackCommandHandler,
    ICommandHandler<SeekCommand> seekCommandHandler,
    ICommandHandler<SetCurrentSessionCommand, CurrentSessionUpdatedDto> setCurrentSessionCommandHandler,
    ICommandHandler<SetPlayingCommand> setPlayingCommandHandler,
    ICommandHandler<SetNextSessionCommand, CurrentSessionUpdatedDto> setNextSessionCommandHandler,
    ICommandHandler<SyncPlaybackSessionCommand, SyncPlaybackSessionResponse> syncPlaybackSessionCommandHandler)
    : ControllerBase
{
    [HttpPost("sync")]
    [ProducesResponseType(typeof(SyncPlaybackSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncPlaybackSession([FromBody] SyncPlaybackSessionCommand command)
    {
        return Ok(await syncPlaybackSessionCommandHandler.HandleAsync(command));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CurrentSessionUpdatedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetCurrentSession([FromBody] SetCurrentSessionCommand command)
    {
        return Ok(await setCurrentSessionCommandHandler.HandleAsync(command));
    }

    [HttpPost("next")]
    [ProducesResponseType(typeof(CurrentSessionUpdatedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetNextSession([FromBody] SetNextSessionCommand command)
    { 
        return Ok(await setNextSessionCommandHandler.HandleAsync(command));
    }

    [HttpDelete]
    [ProducesResponseType(typeof(CurrentSessionUpdatedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCurrentSession()
    {
        return Ok(await clearCurrentSessionCommandHandler.HandleAsync(new ClearCurrentSessionCommand()));
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(GetUsersSessionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> History([FromQuery] GetUsersSessionsQuery query)
    {
        return Ok(await getUsersSessionsQueryHandler.HandleAsync(query));
    }

    [HttpPost("request-playback")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestPlayback([FromBody] RequestPlaybackCommand command)
    {
        await requestPlaybackCommandHandler.HandleAsync(command);
            
        return NoContent();
    }
    
    [HttpPost("request-pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestPause()
    {
        await requestPauseCommandHandler.HandleAsync(new RequestPauseCommand());
            
        return NoContent();
    }

    [HttpPost("playing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetPlaying([FromBody] SetPlayingCommand command)
    {
        await setPlayingCommandHandler.HandleAsync(command);

        return NoContent();
    }
    
    [HttpPost("seek")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Seek([FromBody] SeekRequest command)
    {
        await seekCommandHandler.HandleAsync(new SeekCommand
        {
            Time = TimeSpan.FromSeconds(command.Time)
        });

        return NoContent();
    }
}