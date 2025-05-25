using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Services;

namespace MixServer.Infrastructure.Users.Services;

public class DeviceTrackingService(
    ILogger<DeviceTrackingService> logger,
    IServiceProvider serviceProvider)
    : IDeviceTrackingService
{
    private readonly ConcurrentDictionary<Guid, DeviceState> _states = new();

    public IDeviceState GetDeviceStateOrThrow(Guid deviceId)
    {
        return _states.TryGetValue(deviceId, out var state)
            ? state
            : throw new NotFoundException(nameof(DeviceState), deviceId);
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

    public void UpdateCapabilities(string userId, Guid deviceId, Dictionary<string, bool> capabilities)
    {
        GetOrAdd(userId, deviceId, state =>
        {
            state.UpdateCapabilities(capabilities);
        });
    }

    private void GetOrAdd(string userId, Guid deviceId, Action<DeviceState>? action)
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
    }

    public void Populate(Device device)
    {
        Populate([device]);
    }

    public void Populate(List<Device> devices)
    {
        foreach (var device in devices)
        {
            if (_states.TryGetValue(device.Id, out var deviceState))
            {
                device.Populate(deviceState);
            }
        }
    }

    private async void OnDeviceStateChanged(object? sender, EventArgs eventArgs)
    {
        if (sender is not IDeviceState deviceState)
        {
            return;
        }
        
        try
        {
            using var scope = serviceProvider.CreateScope();
            var callbackService = scope.ServiceProvider.GetRequiredService<ICallbackService>();
        
            logger.LogInformation("Device: {DeviceId} online: {Online} interaction state: {InteractedWith} capabilities: {Capabilities}",
                deviceState.DeviceId,
                deviceState.Online,
                deviceState.InteractedWith,
                string.Join(", ", deviceState.Capabilities.Keys.ToList()));
            await callbackService.DeviceStateUpdated(deviceState);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling device state change for {DeviceId}", deviceState.DeviceId);
        }
    }
}