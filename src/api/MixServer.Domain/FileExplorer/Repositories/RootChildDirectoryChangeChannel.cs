using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models.Indexing;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IRootChildDirectoryChangeChannel : IChannel<RootChildChangeEvent>;

public class RootChildDirectoryChangeChannel(ILogger<RootChildDirectoryChangeChannel> logger) 
    : ChannelBase<RootChildChangeEvent>(logger, deDuplicateRequests: false, singleReader: true, singleWriter: true),
        IRootChildDirectoryChangeChannel;