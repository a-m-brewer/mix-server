using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IPersistFolderCommandChannel : IChannel<PersistFolderCommand>;

public class PersistFolderCommandChannel() : ChannelBase<PersistFolderCommand>(deDuplicateRequests: false), IPersistFolderCommandChannel;