using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IScanFolderRequestChannel : IChannel<ScanFolderRequest>;

public class ScanFolderRequestChannel : ChannelBase<ScanFolderRequest>, IScanFolderRequestChannel;