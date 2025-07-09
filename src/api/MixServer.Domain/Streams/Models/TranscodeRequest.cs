using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.Streams.Models;

public record TranscodeRequest(NodePath Path, Guid TranscodeId, int Bitrate) : IChannelMessage
{
    public string Identifier => Path.AbsolutePath;
}
