using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IRemoveMediaMetadataChannel : IChannel<RemoveMediaMetadataRequest>;

public class RemoveMediaMetadataChannel(ILogger<RemoveMediaMetadataChannel> logger) : ChannelBase<RemoveMediaMetadataRequest>(logger, singleReader: true), IRemoveMediaMetadataChannel;