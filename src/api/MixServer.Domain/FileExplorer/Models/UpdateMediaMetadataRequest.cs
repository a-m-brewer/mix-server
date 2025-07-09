using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Models;

public record UpdateMediaMetadataRequest(List<Guid> FileIds) : IChannelMessage
{
    public string Identifier => GetHashCode().ToString();
}