using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Streams.Models;

namespace MixServer.Domain.Streams.Repositories;

public interface ITranscodeChannel : IChannel<TranscodeRequest>, ISingletonRepository;

public class TranscodeChannel(ILogger<TranscodeChannel> logger) : ChannelBase<TranscodeRequest>(), ITranscodeChannel;