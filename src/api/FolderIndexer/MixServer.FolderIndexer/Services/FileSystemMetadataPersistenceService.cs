using System.Text.RegularExpressions;
using MixServer.FolderIndexer.Domain.Entities;
using MixServer.FolderIndexer.Domain.Repositories;
using MixServer.FolderIndexer.Tags.Factories;
using MixServer.FolderIndexer.Tags.Interface.Interfaces;

namespace MixServer.FolderIndexer.Services;

internal interface IFileSystemMetadataPersistenceService
{
    Task UpdateMetadataAsync(FileInfoEntity fileInfoEntity, FileInfo fileInfo, CancellationToken cancellationToken);
    FileMetadataEntity CreateMetadata(FileInfo fileInfo);
    Task AddAsync(FileMetadataEntity fileMetadata, CancellationToken cancellationToken);
}

internal partial class FileSystemMetadataPersistenceService(
    ITagBuilderFactory tagBuilderFactory,
    IFileSystemMetadataRepository fileSystemMetadataRepository,
    FileExtensionContentTypeProvider mimeTypeProvider) : IFileSystemMetadataPersistenceService
{
    private static readonly HashSet<string> ExcludedMediaMimeTypes =
    [
        "video/vnd.dlna.mpeg-tts"
    ];

    public FileMetadataEntity CreateMetadata(FileInfo fileInfo)
    {
        var (mimeType, isMedia) = GetMimetypeInfo(fileInfo.FullName);

        if (!isMedia)
        {
            return new FileMetadataEntity
            {
                Id = Guid.NewGuid(),
                MimeType = mimeType
            };
        }

        using var tagBuilder = tagBuilderFactory.CreateReadOnly(fileInfo.FullName);

        return new MediaMetadataEntity
        {
            Id = Guid.NewGuid(),
            Bitrate = tagBuilder.Bitrate,
            Duration = tagBuilder.Duration,
            MimeType = mimeType
        };
    }

    public Task AddAsync(FileMetadataEntity fileMetadata, CancellationToken cancellationToken)
    {
        return fileSystemMetadataRepository.AddAsync(fileMetadata, cancellationToken);
    }

    public async Task UpdateMetadataAsync(
        FileInfoEntity fileInfoEntity,
        FileInfo fileInfo,
        CancellationToken cancellationToken)
    {
        var (mimeType, isMedia) = GetMimetypeInfo(fileInfoEntity.FullName);
        
        var wasMedia = fileInfoEntity.Metadata is MediaMetadataEntity;

        // Update
        if (isMedia == wasMedia)
        {
            fileInfoEntity.Metadata.MimeType = mimeType;
            if (fileInfoEntity.Metadata is MediaMetadataEntity mediaMetadataEntity)
            {
                using var tagBuilder = tagBuilderFactory.CreateReadOnly(fileInfoEntity.FullName);
                mediaMetadataEntity.Bitrate = tagBuilder.Bitrate;
                mediaMetadataEntity.Duration = tagBuilder.Duration;
            }
        }
        
        fileSystemMetadataRepository.Remove(fileInfoEntity.Metadata);
        fileInfoEntity.Metadata = CreateMetadata(fileInfo);
        await fileSystemMetadataRepository.AddAsync(fileInfoEntity.Metadata, cancellationToken);
    }

    private (string MimeType, bool IsMedia) GetMimetypeInfo(string fullName)
    {
        var mimeType = mimeTypeProvider.GetContentTypeOrDefault(fullName);
        var isMedia = !string.IsNullOrWhiteSpace(mimeType) &&
                      AudioVideoMimeTypeRegex().IsMatch(mimeType) &&
                      !ExcludedMediaMimeTypes.Contains(mimeType);
        return (mimeType, isMedia);
    }
    
    [GeneratedRegex(@"^(audio|video)\/(.*)")]
    private static partial Regex AudioVideoMimeTypeRegex();
}