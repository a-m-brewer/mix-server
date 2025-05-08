using System.Threading.Channels;

namespace MixServer.Domain.FileExplorer.Services.Indexing;

public class FileSystemIndexerChannelStore
{
    public Channel<(DirectoryInfo Parent, ICollection<FileSystemInfo> Children)> FileSystemInfoChannel { get; } = Channel.CreateUnbounded<(DirectoryInfo Parent, ICollection<FileSystemInfo> Children)>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });
}