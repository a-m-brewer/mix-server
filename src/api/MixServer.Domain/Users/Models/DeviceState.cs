namespace MixServer.Domain.Users.Models;

public interface IDeviceState
{
    Guid DeviceId { get; }
    string LastInteractedWith { get; }
    
    bool InteractedWith { get; }

    void UpdateInteractionState(string userId, bool interactedWith);
}

public class DeviceState(Guid deviceId) : IDeviceState
{
    public event EventHandler? StateChanged;

    public Guid DeviceId { get; } = deviceId;

    public string LastInteractedWith { get; private set; } = string.Empty;

    public bool InteractedWith { get; private set; }

    public void UpdateInteractionState(string userId, bool interactedWith)
    {
        LastInteractedWith = userId;
        InteractedWith = interactedWith;
        
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}