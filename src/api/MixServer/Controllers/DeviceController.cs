using Microsoft.AspNetCore.Mvc;
using MixServer.Application.Devices.Commands.DeleteDevice;
using MixServer.Application.Devices.Commands.SetDeviceInteraction;
using MixServer.Application.Devices.Commands.UpdateDevicePlaybackCapabilities;
using MixServer.Application.Devices.Queries.GetUsersDevices;
using MixServer.Domain.Interfaces;

namespace MixServer.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class DeviceController(    
    ICommandHandler<DeleteDeviceCommand> deleteDeviceCommandHandler,
    IQueryHandler<GetUsersDevicesQueryResponse> getUsersDevicesQueryHandler,
    ICommandHandler<SetDeviceInteractionCommand> setDeviceInteractionCommandHandler,
    ICommandHandler<UpdateDevicePlaybackCapabilitiesCommand> updateDevicePlaybackCapabilitiesCommandHandler)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetUsersDevicesQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Devices(CancellationToken cancellationToken)
    {
        return Ok(await getUsersDevicesQueryHandler.HandleAsync(cancellationToken));
    }

    [HttpDelete("{deviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDevice([FromRoute] Guid deviceId, CancellationToken cancellationToken)
    {
        await deleteDeviceCommandHandler.HandleAsync(new DeleteDeviceCommand
        {
            DeviceId = deviceId
        }, cancellationToken);
        
        return NoContent();
    }
    
    [HttpPost("interacted")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDeviceInteracted([FromBody] SetDeviceInteractionCommand command, CancellationToken cancellationToken)
    {
        await setDeviceInteractionCommandHandler.HandleAsync(command, cancellationToken);
        
        return NoContent();
    }
    
    [HttpPost("capabilities")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDeviceCapabilities([FromBody] UpdateDevicePlaybackCapabilitiesCommand command, CancellationToken cancellationToken)
    {
        await updateDevicePlaybackCapabilitiesCommandHandler.HandleAsync(command, cancellationToken);
        
        return NoContent();
    }
}