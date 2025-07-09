using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IPersistFolderCommandChannel : IChannel<PersistFolderCommand>;

public class PersistFolderCommandChannel(ILogger<PersistFolderCommandChannel> logger) : ChannelBase<PersistFolderCommand>(logger), IPersistFolderCommandChannel;