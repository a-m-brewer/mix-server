using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileWriteLockService
{
    Task WriteAsync(string path, Action action);
}

public class FileWriteLockService(ILogger<FileWriteLockService> logger) : IFileWriteLockService
{
    private class FileWriteLock
    {
        private int _refCount;

        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int RefCount => _refCount;

        public void IncrementRefCount()
        {
            Interlocked.Increment(ref _refCount);
        }

        public bool DecrementRefCount()
        {
            if (Interlocked.Decrement(ref _refCount) != 0)
            {
                return false;
            }

            Semaphore.Dispose();
            return true;
        }
    }
    
    private readonly ConcurrentDictionary<string, FileWriteLock> _locks = new();

    public async Task WriteAsync(string path, Action action)
    {
        var fileLock = _locks.GetOrAdd(path, _ => new FileWriteLock());
        fileLock.IncrementRefCount();

        try
        {
            if (!await fileLock.Semaphore.WaitAsync(TimeSpan.FromSeconds(30)))
            {
                logger.LogWarning("Failed to acquire lock for {Path} after 30 seconds", path);
                return;
            }
            
            try
            {
                action();
            }
            catch (IOException e)
            {
                logger.LogError(e, "IO error while writing to {Path}", path);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while writing to {Path}", path);
            }
            finally
            {
                fileLock.Semaphore.Release();
            }
        }
        finally
        {
            if (fileLock.DecrementRefCount())
            {
                _locks.TryRemove(path, out _);
            }
        }
    }
}