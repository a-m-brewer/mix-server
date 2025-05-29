using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Users.Commands.AddUser;
using MixServer.Application.Users.Commands.DeleteUser;
using MixServer.Application.Users.Commands.LoginUser;
using MixServer.Application.Users.Commands.RefreshUser;
using MixServer.Application.Users.Commands.ResetPassword;
using MixServer.Application.Users.Commands.UpdateUser;
using MixServer.Application.Users.Queries.GetAllUsers;
using MixServer.Auth;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class UserController(
    ICommandHandler<AddUserCommand, AddUserCommandResponse> addUserCommandHandler,
    ICommandHandler<DeleteUserCommand> deleteUserCommandHandler,
    IQueryHandler<GetAllUsersResponse> getAllUsersQueryHandler,
    ICommandHandler<LoginUserCommand, LoginCommandResponse> loginUserCommandHandler,
    ICommandHandler<ResetPasswordCommand> resetPasswordCommandHandler,
    ICommandHandler<RefreshUserCommand, RefreshUserResponse> refreshUserCommandHandler,
    ICommandHandler<UpdateUserCommand> updateUserCommandHandler)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.IsAdminOrOwner)]
    [ProducesResponseType(typeof(GetAllUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await getAllUsersQueryHandler.HandleAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Policies.IsAdminOrOwner)]
    [ProducesResponseType(typeof(AddUserCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddUser([FromBody] AddUserCommand command, CancellationToken cancellationToken)
    {
        return Ok(await addUserCommandHandler.HandleAsync(command, cancellationToken));
    }
    
    [HttpPut("{userId}")]
    [Authorize(Policy = Policies.IsAdminOrOwner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser([FromRoute] string userId, [FromBody] UpdateUserCommand command, CancellationToken cancellationToken)
    {
        command.UserId = userId;
        
        await updateUserCommandHandler.HandleAsync(command, cancellationToken);
        
        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    [Authorize(Policy = Policies.IsAdminOrOwner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser([FromRoute] string userId, CancellationToken cancellationToken)
    {
        await deleteUserCommandHandler.HandleAsync(new DeleteUserCommand
        {
            UserId = userId
        }, cancellationToken);
        
        return NoContent();
    }
    
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken cancellationToken)
    {
        return Ok(await loginUserCommandHandler.HandleAsync(command, cancellationToken));
    }

    [HttpPost("reset")]
    [Authorize(Policy = Policies.PasswordReset)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await resetPasswordCommandHandler.HandleAsync(command, cancellationToken);
        
        return NoContent();
    }
    
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshUserCommand command, CancellationToken cancellationToken)
    {
        return Ok(await refreshUserCommandHandler.HandleAsync(command, cancellationToken));
    }
}