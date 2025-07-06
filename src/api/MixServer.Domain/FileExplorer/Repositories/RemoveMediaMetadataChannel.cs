using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IRemoveMediaMetadataChannel : IChannel<RemoveMediaMetadataRequest>;

public class RemoveMediaMetadataChannel() : ChannelBase<RemoveMediaMetadataRequest>(singleReader: true), IRemoveMediaMetadataChannel;