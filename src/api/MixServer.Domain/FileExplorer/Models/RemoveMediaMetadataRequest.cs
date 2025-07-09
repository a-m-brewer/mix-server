using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Models;

public record RemoveMediaMetadataRequest(List<NodePath> NodePaths) : IChannelMessage
{
    public string Identifier => GetHashCode().ToString();
}