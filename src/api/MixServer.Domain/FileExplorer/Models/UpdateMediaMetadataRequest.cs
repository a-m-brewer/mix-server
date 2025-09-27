using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Models;

public record UpdateMediaMetadataRequest(NodePath ParentNodePath) : IChannelMessage
{
    public string Identifier => $"UpdateMediaMetadata:{ParentNodePath.AbsolutePath}";
}