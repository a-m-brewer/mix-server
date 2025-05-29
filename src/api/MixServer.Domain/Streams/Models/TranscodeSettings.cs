namespace MixServer.Domain.Streams.Models;

public class TranscodeSettings
{
    public int HlsTimeInSeconds { get; set; } = 4;
    
    public int DefaultBitrate { get; set; } = 192;
}