using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IUpdateMediaMetadataChannel : IChannel<UpdateMediaMetadataRequest>;

public class UpdateMediaMetadataChannelBase(ILogger<UpdateMediaMetadataChannelBase> logger) : ChannelBase<UpdateMediaMetadataRequest>(), IUpdateMediaMetadataChannel;