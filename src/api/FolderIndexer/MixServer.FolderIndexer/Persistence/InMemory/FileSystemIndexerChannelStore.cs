using System.Threading.Channels;

namespace MixServer.FolderIndexer.Persistence.InMemory;

internal class FileSystemIndexerChannelStore
{
    public Channel<string> ScannerChannel { get; } = Channel.CreateBounded<string>(new BoundedChannelOptions(1)
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });
    
    public Channel<(DirectoryInfo Parent, ICollection<FileSystemInfo> Children)> FileSystemInfoChannel { get; } = 
        Channel.CreateBounded<(DirectoryInfo Parent, ICollection<FileSystemInfo> Children)>(new BoundedChannelOptions(1)
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });
}