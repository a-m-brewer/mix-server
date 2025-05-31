using MixServer.Domain.FileExplorer.Models.Indexing;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IRootChildDirectoryChangeChannel : IChannel<RootChildChangeEvent>;

public class RootChildDirectoryChangeChannel() 
    : ChannelBase<RootChildChangeEvent>(deDuplicateRequests: false, singleReader: true, singleWriter: true),
        IRootChildDirectoryChangeChannel;