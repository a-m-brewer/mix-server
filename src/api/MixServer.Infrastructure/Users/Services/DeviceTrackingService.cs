using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Services;
using MixServer.Domain.Utilities;

namespace MixServer.Infrastructure.Users.Services;

public class DeviceTrackingService(
    ILogger<DeviceTrackingService> logger,
    IReadWriteLock readWriteLock,
    IServiceProvider serviceProvider)
    : IDeviceTrackingService
{
    private readonly Dictionary<Guid, DeviceState> _states = new();

    public bool DeviceInteractedWith(Guid deviceId)
    {
        return readWriteLock.ForRead(() => _states.TryGetValue(deviceId, out var state) && state.InteractedWith);
    }

    public void SetOnline(string userId, Guid deviceId, bool online)
    {
        GetOrAdd(userId, deviceId, state =>
        {
            logger.LogInformation("User: {UserId} device: {DeviceId} online: {Online}",
                userId,
                deviceId,
                online);
            state.SetOnline(online);
        });
    }

    public void SetInteraction(string userId, Guid deviceId, bool interactedWith)
    {
        GetOrAdd(userId, deviceId, state =>
        {
            logger.LogInformation("User: {UserId} device: {DeviceId} interacted with: {InteractedWith}",
                userId,
                deviceId,
                interactedWith);
            state.UpdateInteractionState(interactedWith);
        });
    }

    private void GetOrAdd(string userId, Guid deviceId, Action<DeviceState>? action)
    {
        readWriteLock.ForWrite(() =>
        {
            if (_states.TryGetValue(deviceId, out var state))
            {
                action?.Invoke(state);
            }
            else
            {
                var deviceState = new DeviceState(deviceId);
                deviceState.StateChanged += OnDeviceStateChanged;

                deviceState.LastInteractedWith = userId;
                action?.Invoke(deviceState);
                
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
        readWriteLock.ForRead(() =>
        {
            foreach (var device in devices)
            {
                if (_states.TryGetValue(device.Id, out var deviceState))
                {
                    device.Populate(deviceState);
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
        
        using var scope = serviceProvider.CreateScope();
        var callbackService = scope.ServiceProvider.GetRequiredService<ICallbackService>();
        
        logger.LogInformation("Device: {DeviceId} online: {Online} interaction state: {InteractedWith}",
            deviceState.DeviceId,
            deviceState.Online,
            deviceState.InteractedWith);
        await callbackService.DeviceStateUpdated(deviceState);
    }
}