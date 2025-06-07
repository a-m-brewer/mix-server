using System.Text.Json;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Constants;
using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileSystemFolderMetadataService
{
    Task<FileSystemFolderMetadataFileDto> GetOrCreateAsync(NodePath nodePath, CancellationToken cancellationToken = default);
    Task<FileSystemFolderMetadataFileDto?> GetOrDefaultAsync(NodePath nodePath, CancellationToken cancellationToken);

    Task<FileSystemFolderMetadataFileDto> CreateMetadataAsync(NodePath nodePath, Guid? folderId = null, CancellationToken cancellationToken = default);
}

public class FileSystemFolderMetadataService(ILogger<FileSystemFolderMetadataService> logger) : IFileSystemFolderMetadataService
{
    public async Task<FileSystemFolderMetadataFileDto> GetOrCreateAsync(NodePath nodePath, CancellationToken cancellationToken = default)
    {
        var metadata = await GetOrDefaultAsync(nodePath, cancellationToken);
        if (metadata is not null)
        {
            return metadata;
        }

        return await CreateMetadataAsync(nodePath, cancellationToken: cancellationToken);
    }

    public async Task<FileSystemFolderMetadataFileDto?> GetOrDefaultAsync(NodePath nodePath, CancellationToken cancellationToken = default)
    {
        var metadataPath = GetMetadataPath(nodePath);
        if (!File.Exists(metadataPath))
        {
            return null;
        }
        
        try
        {
            await using var stream = File.OpenRead(metadataPath);
            var metadata = await JsonSerializer.DeserializeAsync<FileSystemFolderMetadataFileJson>(stream, cancellationToken: cancellationToken);
            if (metadata is null)
            {
                return null;
            }

            return new FileSystemFolderMetadataFileDto
            {
                Path = nodePath,
                FolderId = metadata.FolderId
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read metadata for {NodePath}", nodePath.AbsolutePath);
            return null;
        }
    }

    public async Task<FileSystemFolderMetadataFileDto> CreateMetadataAsync(NodePath nodePath, Guid? folderId = null, CancellationToken cancellationToken = default)
    {
        var metadataPath = GetMetadataPath(nodePath);
        var metadataJson = new FileSystemFolderMetadataFileJson
        {
            FolderId = folderId ?? Guid.NewGuid(),
        };
        try
        {
            await using var stream = File.OpenWrite(metadataPath);
            await JsonSerializer.SerializeAsync(stream, metadataJson, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create metadata file for {NodePath}", nodePath.AbsolutePath);
            throw;
        }
        
        return new FileSystemFolderMetadataFileDto
        {
            Path = nodePath,
            FolderId = metadataJson.FolderId
        };
    }

    private static string GetMetadataPath(NodePath nodePath) => Path.Join(nodePath.AbsolutePath, FolderMetadataConstants.MetadataFileName);
}