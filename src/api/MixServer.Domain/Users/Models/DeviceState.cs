namespace MixServer.Domain.Users.Models;

public interface IDeviceState
{
    Guid DeviceId { get; }
    string LastInteractedWith { get; }
    
    bool InteractedWith { get; }
    
    bool Online { get;}
}

public class DeviceState(Guid deviceId) : IDeviceState
{
    public event EventHandler? StateChanged;

    public Guid DeviceId { get; } = deviceId;

    public string LastInteractedWith { get; set; } = string.Empty;

    public bool Online { get; private set; }
    
    public bool InteractedWith { get; private set; }
    
    public void SetOnline(bool online)
    {
        Online = online;
        
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateInteractionState(bool interactedWith)
    {
        InteractedWith = interactedWith;
        
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}