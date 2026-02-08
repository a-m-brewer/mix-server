using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Domain.Tracklists.Services;

namespace MixServer.Application.FileExplorer.Commands.UpdateMediaMetadata;

public class UpdateMediaMetadataCommandHandler(
    ICallbackService callbackService,
    ILogger<UpdateMediaMetadataCommandHandler> logger,
    IMediaInfoCache mediaInfoCache,
    ITagBuilderFactory tagBuilderFactory,
    ITracklistTagService tracklistTagService) : ICommandHandler<UpdateMediaMetadataRequest>
{
    public async Task HandleAsync(UpdateMediaMetadataRequest request, CancellationToken cancellationToken)
    {
        var mediaInfos = new ConcurrentBag<MediaInfo>();

        var transformBlock = new TransformBlock<NodePath, MediaInfo?>(path =>
            {
                try
                {
                    using var tb = tagBuilderFactory.CreateReadOnly(path.AbsolutePath);
                    var tracklist = tracklistTagService.GetTracklist(tb);
                    return new MediaInfo
                    {
                        Bitrate = tb.Bitrate,
                        Duration = tb.Duration,
                        Tracklist = tracklist,
                        Path = path
                    };
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error processing file {Path}", path.AbsolutePath);
                    return null;
                }
            },
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });

        var actionBlock = new ActionBlock<MediaInfo?>(mediaInfo =>
        {
            if (mediaInfo is null)
            {
                return;
            }
            
            mediaInfos.Add(mediaInfo);
        }, new ExecutionDataflowBlockOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
        });

        transformBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

        foreach (var path in request.NodePaths)
        {
            await transformBlock.SendAsync(path, cancellationToken);
        }

        transformBlock.Complete();
        await actionBlock.Completion;
        
        var mediaInfosList = mediaInfos.ToList();

        mediaInfoCache.AddOrReplace(mediaInfosList);
        await callbackService.MediaInfoUpdated(mediaInfosList);
    }
}