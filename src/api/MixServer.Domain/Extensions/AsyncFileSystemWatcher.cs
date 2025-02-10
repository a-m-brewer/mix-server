namespace MixServer.Domain.Extensions;

public static class AsyncFileSystemWatcher
{
    public static Task<string> WaitForFileAsync(string directory,string fileName, TimeSpan timeout, CancellationToken token = default)
    {
        var tcs = new TaskCompletionSource<string>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        
        if (timeout != Timeout.InfiniteTimeSpan)
        {
            cts.CancelAfter(timeout);
        }
        
        var watcher = new FileSystemWatcher(directory)
        {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
        };
        
        using var registration = cts.Token.Register(() =>
        {
            watcher.Dispose();
            tcs.TrySetCanceled();
        });
         
        watcher.Created += (_, file) =>
        {
            if (file.Name != fileName)
            {
                return;
            }

            watcher.Dispose();
            tcs.TrySetResult(file.FullPath);
        };

        return tcs.Task;
    }
}