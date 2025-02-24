namespace MixServer.Domain.Streams.Entities;

public class Transcode
{
    public Guid Id { get; set; }
    
    public string AbsolutePath { get; set; } = string.Empty;
}