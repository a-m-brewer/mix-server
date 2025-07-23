using System.Security.Cryptography;
using System.Text;

namespace MixServer.Domain.FileExplorer.Services;

public class FsHashBuilder : IDisposable
{
    private readonly MD5 _md5;

    public FsHashBuilder()
    {
        _md5 = MD5.Create();
    }

    public FsHashBuilder Add(FileSystemInfo info)
    {
        var typeMarker = info is FileInfo ? "F" : "D";
        var hashString =  $"{typeMarker}:{info.FullName}:{info.LastWriteTimeUtc.Ticks}";
        var hashBytes = Encoding.UTF8.GetBytes(hashString);

        _md5.TransformBlock(hashBytes, 0, hashBytes.Length, null, 0);
        
        return this;
    }

    public string ComputeHash()
    {
        _md5.TransformFinalBlock([], 0, 0);
        if (_md5.Hash is null)
        {
            return string.Empty;
        }
        
        return BitConverter.ToString(_md5.Hash)
            .Replace("-", "")
            .ToLowerInvariant();
    }
    
    public void Dispose()
    {
        _md5.Dispose();
    }
}