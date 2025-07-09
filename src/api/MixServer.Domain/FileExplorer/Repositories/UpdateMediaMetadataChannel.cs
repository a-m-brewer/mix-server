using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IUpdateMediaMetadataChannel : IChannel<UpdateMediaMetadataRequest>;

public class UpdateMediaMetadataChannel(ILogger<UpdateMediaMetadataChannel> logger) : ChannelBase<UpdateMediaMetadataRequest>(logger, singleReader: true), IUpdateMediaMetadataChannel;