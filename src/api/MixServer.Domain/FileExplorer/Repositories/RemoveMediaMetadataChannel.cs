using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IRemoveMediaMetadataChannel : IChannel<RemoveMediaMetadataRequest>;

public class RemoveMediaMetadataChannelBase(ILogger<RemoveMediaMetadataChannelBase> logger) : ChannelBase<RemoveMediaMetadataRequest>, IRemoveMediaMetadataChannel;