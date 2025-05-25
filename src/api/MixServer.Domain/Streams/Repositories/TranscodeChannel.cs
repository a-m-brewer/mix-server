using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Streams.Models;

namespace MixServer.Domain.Streams.Repositories;

public interface ITranscodeChannel : IChannel<TranscodeRequest>, ISingletonRepository;

public class TranscodeChannel : ChannelBase<TranscodeRequest>, ITranscodeChannel;