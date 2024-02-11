using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Services;
using MixServer.Domain.Utilities;

namespace MixServer.Infrastructure.Users.Services;

public class DeviceTrackingService : IDeviceTrackingService
{
    private readonly Dictionary<Guid, DeviceState> _states = new();

    private readonly ILogger<DeviceTrackingService> _logger;
    private readonly IReadWriteLock _readWriteLock;
    private readonly IServiceProvider _serviceProvider;

    public DeviceTrackingService(
        ILogger<DeviceTrackingService> logger,
        IReadWriteLock readWriteLock,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _readWriteLock = readWriteLock;
        _serviceProvider = serviceProvider;
    }

    public bool DeviceInteractedWith(Guid deviceId)
    {
        return _readWriteLock.ForRead(() => _states.TryGetValue(deviceId, out var state) && state.InteractedWith);
    }

    public void SetInteraction(string userId, Guid deviceId, bool interactedWith)
    {
        _readWriteLock.ForWrite(() =>
        {
            _logger.LogInformation("User: {UserId} device: {DeviceId} interacted with: {InteractedWith}",
                userId,
                deviceId,
                interactedWith);
            if (_states.TryGetValue(deviceId, out var state))
            {
                state.UpdateInteractionState(userId, interactedWith);
            }
            else
            {
                var deviceState = new DeviceState(deviceId);
                deviceState.StateChanged += OnDeviceStateChanged;

                deviceState.UpdateInteractionState(userId, interactedWith);
                
                _states[deviceId] = deviceState;
            }
        });
    }

    public void Populate(Device device)
    {
        Populate([device]);
    }

    public void Populate(List<Device> devices)
    {
        _readWriteLock.ForRead(() =>
        {
            foreach (var device in devices)
            {
                if (_states.TryGetValue(device.Id, out var deviceState))
                {
                    device.UpdateInteractionState(deviceState.LastInteractedWith, deviceState.InteractedWith);
                }
            }
        });
    }

    private async void OnDeviceStateChanged(object? sender, EventArgs eventArgs)
    {
        if (sender is not IDeviceState deviceState)
        {
            return;
        }
        
        using var scope = _serviceProvider.CreateScope();
        var callbackService = scope.ServiceProvider.GetRequiredService<ICallbackService>();
        
        _logger.LogInformation("Device: {DeviceId} interaction state: {InteractedWith}",
            deviceState.DeviceId,
            deviceState.InteractedWith);
        await callbackService.DeviceStateUpdated(deviceState);
    }
}