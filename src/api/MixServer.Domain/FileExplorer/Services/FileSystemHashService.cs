using System.Security.Cryptography;
using System.Text;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileSystemHashService
{
    Task<string> ComputeFileMd5HashAsync(
        NodePath nodePath,
        CancellationToken cancellationToken = default);

    Task<string> ComputeFolderMd5HashAsync(
        DirectoryInfo directoryInfo,
        CancellationToken cancellationToken = default);

    Task<string> ComputeFolderMd5HashAsync(
        NodePath nodePath,
        CancellationToken cancellationToken = default);
}

public class FileSystemHashService : IFileSystemHashService
{
    public async Task<string> ComputeFileMd5HashAsync(
        NodePath nodePath,
        CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.OpenRead(nodePath.AbsolutePath);
        return await ComputeMd5HashAsync(fileStream, cancellationToken);
    }

    public async Task<string> ComputeFolderMd5HashAsync(
        DirectoryInfo directoryInfo,
        CancellationToken cancellationToken = default)
    {
        var childrenHashes = directoryInfo
            .MsEnumerateFileSystemInfos()
            .Select(ToHashString);
        
        var allPaths = childrenHashes
            .OrderBy(o => o, StringComparer.Ordinal)
            .ToList();
        
        var joinedPaths = string.Join(";", allPaths);
        var bytes = Encoding.UTF8.GetBytes(joinedPaths);
        await using var memoryStream = new MemoryStream(bytes);
        return await ComputeMd5HashAsync(memoryStream, cancellationToken);

        string ToHashString(FileSystemInfo info)
        {
            var typeMarker = info is FileInfo ? "F" : "D";
            return $"{typeMarker}:{info.FullName}:{info.LastWriteTimeUtc.Ticks}";
        }
    }

    public Task<string> ComputeFolderMd5HashAsync(NodePath nodePath, CancellationToken cancellationToken = default)
    {
        var directoryInfo = new DirectoryInfo(nodePath.AbsolutePath);
        return ComputeFolderMd5HashAsync(directoryInfo, cancellationToken);
    }

    private static async Task<string> ComputeMd5HashAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        using var md5 = MD5.Create();
        return await ComputeHashAsync(fileStream, md5, cancellationToken);
    }
    
    private static async Task<string> ComputeHashAsync(
        Stream fileStream,
        HashAlgorithm hashAlgorithm,
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await fileStream.ReadAsync(buffer, cancellationToken)) != 0)
        {
            hashAlgorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
        }
        hashAlgorithm.TransformFinalBlock(buffer, 0, 0);

        if (hashAlgorithm.Hash is null)
        {
            return string.Empty;
        }
        
        return BitConverter.ToString(hashAlgorithm.Hash)
            .Replace("-", "")
            .ToLowerInvariant();
    }
}