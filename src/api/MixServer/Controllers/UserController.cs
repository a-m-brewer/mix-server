using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Users.Commands.AddUser;
using MixServer.Application.Users.Commands.DeleteDevice;
using MixServer.Application.Users.Commands.DeleteUser;
using MixServer.Application.Users.Commands.LoginUser;
using MixServer.Application.Users.Commands.RefreshUser;
using MixServer.Application.Users.Commands.ResetPassword;
using MixServer.Application.Users.Commands.UpdateUser;
using MixServer.Application.Users.Queries.GetAllUsers;
using MixServer.Application.Users.Queries.GetUsersDevices;
using MixServer.Auth;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class UserController(
    ICommandHandler<AddUserCommand, AddUserCommandResponse> addUserCommandHandler,
    ICommandHandler<DeleteDeviceCommand> deleteDeviceCommandHandler,
    ICommandHandler<DeleteUserCommand> deleteUserCommandHandler,
    IQueryHandler<GetAllUsersResponse> getAllUsersQueryHandler,
    IQueryHandler<GetUsersDevicesQueryResponse> getUsersDevicesQueryHandler,
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
    public async Task<IActionResult> GetAll()
    {
        return Ok(await getAllUsersQueryHandler.HandleAsync());
    }

    [HttpPost]
    [Authorize(Policy = Policies.IsAdminOrOwner)]
    [ProducesResponseType(typeof(AddUserCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddUser([FromBody] AddUserCommand command)
    {
        return Ok(await addUserCommandHandler.HandleAsync(command));
    }
    
    [HttpPut("{userId}")]
    [Authorize(Policy = Policies.IsAdminOrOwner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser([FromRoute] string userId, [FromBody] UpdateUserCommand command)
    {
        command.UserId = userId;
        
        await updateUserCommandHandler.HandleAsync(command);
        
        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    [Authorize(Policy = Policies.IsAdminOrOwner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser([FromRoute] string userId)
    {
        await deleteUserCommandHandler.HandleAsync(new DeleteUserCommand
        {
            UserId = userId
        });
        
        return NoContent();
    }
    
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        return Ok(await loginUserCommandHandler.HandleAsync(command));
    }

    [HttpPost("reset")]
    [Authorize(Policy = Policies.PasswordReset)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await resetPasswordCommandHandler.HandleAsync(command);
        
        return NoContent();
    }
    
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshUserCommand command)
    {
        return Ok(await refreshUserCommandHandler.HandleAsync(command));
    }

    [HttpGet("device")]
    [ProducesResponseType(typeof(GetUsersDevicesQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Devices()
    {
        return Ok(await getUsersDevicesQueryHandler.HandleAsync());
    }

    [HttpDelete("device/{deviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDevice([FromRoute] Guid deviceId)
    {
        await deleteDeviceCommandHandler.HandleAsync(new DeleteDeviceCommand
        {
            DeviceId = deviceId
        });
        
        return NoContent();
    }
}