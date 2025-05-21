using System.Threading.Channels;

namespace MixServer.FolderIndexer.Persistence.InMemory;

internal class FileSystemIndexerChannelStore
{
    public Channel<string> ScannerChannel { get; } = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });
    
    public Channel<(DirectoryInfo Parent, ICollection<FileSystemInfo> Children)> FileSystemInfoChannel { get; } = 
        Channel.CreateUnbounded<(DirectoryInfo Parent, ICollection<FileSystemInfo> Children)>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });
}