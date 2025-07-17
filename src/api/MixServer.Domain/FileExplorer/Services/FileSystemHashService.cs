using System.Security.Cryptography;
using System.Text;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileSystemHashService
{
    Task<string> ComputeFolderMd5HashAsync(
        DirectoryInfo directoryInfo,
        CancellationToken cancellationToken = default);
}

public class FileSystemHashService : IFileSystemHashService
{
    public Task<string> ComputeFolderMd5HashAsync(
        DirectoryInfo directoryInfo,
        CancellationToken cancellationToken = default)
    {
        using var md5 = MD5.Create();
    
        foreach (var hashString in EnumerateChildHashes(directoryInfo).OrderBy(o => o, StringComparer.Ordinal))
        {
            var hashBytes = Encoding.UTF8.GetBytes(hashString);
            md5.TransformBlock(hashBytes, 0, hashBytes.Length, null, 0);
        
            // Check for cancellation periodically
            cancellationToken.ThrowIfCancellationRequested();
        }
    
        md5.TransformFinalBlock([], 0, 0);
    
        if (md5.Hash is null)
        {
            return Task.FromResult(string.Empty);
        }
    
        return Task.FromResult(BitConverter.ToString(md5.Hash)
            .Replace("-", "")
            .ToLowerInvariant());
    }

    private IEnumerable<string> EnumerateChildHashes(DirectoryInfo directoryInfo)
    {
        foreach (var info in directoryInfo.MsEnumerateFileSystemInfos())
        {
            yield return ToHashString(info);
        }

        yield break;

        string ToHashString(FileSystemInfo info)
        {
            var typeMarker = info is FileInfo ? "F" : "D";
            return $"{typeMarker}:{info.FullName}:{info.LastWriteTimeUtc.Ticks}";
        }
    }
}