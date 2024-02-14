using DeviceDetectorNET;
using MixServer.Domain.Users.Enums;
using ClientType = MixServer.Domain.Users.Enums.ClientType;
using DeviceDetectorClientType = DeviceDetectorNET.Parser.Client.ClientType;

namespace MixServer.Infrastructure.Users.Services;

public class DeviceDetectorWrapper(string? userAgent = "", ClientHints? clientHints = null)
    : DeviceDetector(userAgent, clientHints)
{
    public DeviceType DeviceType => device.HasValue
        ? (DeviceType)device.Value
        : DeviceType.Unknown;

    public ClientType ClientType => GetClientType();

    private ClientType GetClientType()
    {
        if (Is(DeviceDetectorClientType.Browser))
        {
            return ClientType.Browser;
        }

        if (Is(DeviceDetectorClientType.FeedReader))
        {
            return ClientType.FeedReader;
        }

        if (Is(DeviceDetectorClientType.MediaPlayer))
        {
            return ClientType.MediaPlayer;
        }

        if (Is(DeviceDetectorClientType.MobileApp))
        {
            return ClientType.MobileApp;
        }

        if (Is(DeviceDetectorClientType.Library))
        {
            return ClientType.Library;
        }

        if (Is(DeviceDetectorClientType.PIM))
        {
            return ClientType.Pim;
        }

        return ClientType.Unknown;
    }
}