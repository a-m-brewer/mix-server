using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Tracklists.Builders;
using MixServer.Domain.Tracklists.Converters;
using MixServer.Domain.Tracklists.Entities;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Domain.Tracklists.Repositories;
using MixServer.Domain.Tracklists.Services;

namespace MixServer.Application.FileExplorer.Commands.UpdateMediaMetadata;

public class UpdateMediaMetadataCommandHandler(
    IFileExplorerNodeRepository fileExplorerNodeRepository,
    IFileMetadataRepository fileMetadataRepository,
    ILogger<UpdateMediaMetadataCommandHandler> logger,
    IMediaMetadataEntityConverter mediaMetadataEntityConverter,
    IMimeTypeService mimeTypeService,
    ITagBuilderFactory tagBuilderFactory,
    ITracklistConverter tracklistConverter,
    ITracklistFileTaggingService tracklistFileTaggingService,
    ITracklistRepository tracklistRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateMediaMetadataRequest>
{
    private record MetadataUpdateResult(
        FileExplorerFileNodeEntity File,
        AddMediaMetadataRequest? AddedMetadata,
        TracklistEntity? AddedTracklist);
    
    private class MetadataResultsBag : ConcurrentBag<MetadataUpdateResult>
    {
        public List<AddMediaMetadataRequest> AddedMetadata => this
            .Where(x => x.AddedMetadata is not null)
            .Select(x => x.AddedMetadata!)
            .ToList();
        
        public List<TracklistEntity> AddedTracklists => this
            .Where(x => x.AddedTracklist is not null)
            .Select(x => x.AddedTracklist!)
            .ToList();
    }
    
    public async Task HandleAsync(UpdateMediaMetadataRequest request, CancellationToken cancellationToken)
    {
        var results = new MetadataResultsBag();
        await foreach (var file in fileExplorerNodeRepository.GetAllMediaFileNodesInFolderAsync(request.ParentNodePath)
                           .WithCancellation(cancellationToken))
        {
            try
            {
                var result = UpdateFileMetadata(file);
                results.Add(result);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error updating metadata for file {FilePath}", file.Path.AbsolutePath);
            }
        }
        
        logger.LogDebug("Metadata update block completed for {FileCount} files", results.Count);
        
        await fileMetadataRepository.AddRangeAsync(results.AddedMetadata, cancellationToken);
        await tracklistRepository.AddRangeAsync(results.AddedTracklists, cancellationToken);

        var files = results.Select(x => x.File).ToList();
        
        unitOfWork.InvokeCallbackOnSaved(cb => cb.MediaInfoUpdated(ToMediaInfo(files)));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Metadata update completed for folder: {FolderPath} - {FileCount} files", request.ParentNodePath.AbsolutePath, results.Count);
    }

    private List<MediaInfo> ToMediaInfo(IReadOnlyCollection<FileExplorerFileNodeEntity> files)
    {
        var mediaInfos = new List<MediaInfo>();
        foreach (var file in files)
        {
            if (file.Metadata is MediaMetadataEntity mediaMetadata)
            {
                mediaInfos.Add(mediaMetadataEntityConverter.Convert(file.Path, mediaMetadata, file.Tracklist));
            }
        }
        
        return mediaInfos;
    }

    private MetadataUpdateResult  UpdateFileMetadata(FileExplorerFileNodeEntity fileEntity)
    {
        logger.LogTrace("Updating metadata for file {FilePath}", fileEntity.Path.AbsolutePath);
        using var tb = tagBuilderFactory.CreateReadOnly(fileEntity.Path.AbsolutePath);

        var addedMetadata = UpdateFileMetadata(fileEntity, tb);
        var addedTracklist =  ImportTracklistIfNotFound(fileEntity, tb);
        logger.LogDebug("Updated metadata for file {FilePath}", fileEntity.Path.AbsolutePath);
        
        return new MetadataUpdateResult(fileEntity, addedMetadata, addedTracklist);
    }

    private AddMediaMetadataRequest? UpdateFileMetadata(
        FileExplorerFileNodeEntity fileEntity,
        IReadOnlyTagBuilder tb)
    {
        var mimeType = mimeTypeService.GetMimeType(fileEntity.Path);

        if (fileEntity.Metadata is not MediaMetadataEntity mediaMetadataEntity)
        {
            FileMetadataEntity? removed = null;
            
            // EF implementation detail requires a new instance when changing entity type
            if (fileEntity.Metadata is not null)
            {
                removed = fileEntity.Metadata;
            }

            var added = new MediaMetadataEntity
            {
                Bitrate = tb.Bitrate,
                Duration = tb.Duration,
                Id = fileEntity.Metadata?.Id ?? Guid.NewGuid(),
                MimeType = mimeType,
                IsMedia = true,
                Node = fileEntity
            };
            fileEntity.Metadata = added;
            return new AddMediaMetadataRequest(added, removed);
        }

        mediaMetadataEntity.Bitrate = tb.Bitrate;
        mediaMetadataEntity.Duration = tb.Duration;
        mediaMetadataEntity.MimeType = mimeType;

        return null;
    }
    
    private TracklistEntity? ImportTracklistIfNotFound(FileExplorerFileNodeEntity fileEntity, IReadOnlyTagBuilder tb)
    {
        var tracklist = tracklistFileTaggingService.GetTracklist(tb);

        // The DB is becoming the source of truth for tracklists, so this only acts to import existing file based tracklists
        if (fileEntity.Tracklist is not null)
        {
            return null;
        }
        
        fileEntity.Tracklist = tracklistConverter.Convert(tracklist, fileEntity);;

        return fileEntity.Tracklist;
    }
}