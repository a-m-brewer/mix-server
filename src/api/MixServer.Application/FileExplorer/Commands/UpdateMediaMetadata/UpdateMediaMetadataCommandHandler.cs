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
        // logger.LogDebug("Updating media metadata for {Path}", request.NodePath);
        
        using var tb = tagBuilderFactory.CreateReadOnly(request.NodePath.AbsolutePath);
        var tracklist = tracklistTagService.GetTracklist(tb);
        var mediaInfo = new MediaInfo
        {
            Bitrate = tb.Bitrate,
            Duration = tb.Duration,
            Tracklist = tracklist,
            Path = request.NodePath
        };
        
        mediaInfoCache.AddOrReplace([mediaInfo]);

        await callbackService.MediaInfoUpdated([mediaInfo]);
    }
}