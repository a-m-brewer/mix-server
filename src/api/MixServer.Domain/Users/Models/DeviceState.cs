namespace MixServer.Domain.Users.Models;

public interface IDeviceState
{
    Guid DeviceId { get; }
    string LastInteractedWith { get; }
    
    bool InteractedWith { get; }
    
    bool Online { get;}

    Dictionary<string, bool> Capabilities { get; }
}

public class DeviceState(Guid deviceId) : IDeviceState
{
    public event EventHandler? StateChanged;

    public Guid DeviceId { get; } = deviceId;

    public string LastInteractedWith { get; set; } = string.Empty;

    public bool Online { get; private set; }
    
    public bool InteractedWith { get; private set; }
    
    public Dictionary<string, bool> Capabilities { get; } = new();
    
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
    
    public void UpdateCapabilities(Dictionary<string, bool> capabilities)
    {
        var changed = false;
        
        foreach (var (mimeType, supported) in capabilities)
        {
            var hasCapability = Capabilities.ContainsKey(mimeType);
            var capabilityChanged = hasCapability && Capabilities[mimeType] != supported;

            if (capabilityChanged)
            {
                changed = true;
            }
            
            Capabilities[mimeType] = supported;
        }
        
        if (changed)
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}