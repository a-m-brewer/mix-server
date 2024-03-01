using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Users.Commands.DeleteDevice;
using MixServer.Application.Users.Commands.SetDeviceInteraction;
using MixServer.Application.Users.Queries.GetUsersDevices;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class DeviceController(    
    ICommandHandler<DeleteDeviceCommand> deleteDeviceCommandHandler,
    IQueryHandler<GetUsersDevicesQueryResponse> getUsersDevicesQueryHandler,
    ICommandHandler<SetDeviceInteractionCommand> setDeviceInteractionCommandHandler)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetUsersDevicesQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Devices()
    {
        return Ok(await getUsersDevicesQueryHandler.HandleAsync());
    }

    [HttpDelete("{deviceId:guid}")]
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
    
    [HttpPost("interacted")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDeviceInteracted()
    {
        await setDeviceInteractionCommandHandler.HandleAsync(new SetDeviceInteractionCommand
        {
            Interacted = true
        });
        
        return NoContent();
    }
}