using System.Diagnostics;
using MixServer.Domain.Persistence;
using MixServer.Domain.Utilities;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IFolderScanTrackingStore : ISingletonRepository
{
    event EventHandler ScanInProgressChanged;
    TimeSpan ScanDuration { get; }
    bool ScanInProgress { get; set; }
}

public class FolderScanTrackingStore : IFolderScanTrackingStore
{
    private readonly ReadWriteLock _lock = new();
    private readonly Stopwatch _stopwatch = new();
    
    private bool _scanInProgress;

    public event EventHandler? ScanInProgressChanged;
    public TimeSpan ScanDuration => _lock.ForRead(() => _stopwatch.Elapsed);

    public bool ScanInProgress
    {
        get => _lock.ForRead(() => _scanInProgress);
        set
        {
            _lock.ForUpgradeableRead(() =>
            {
                if (value == _scanInProgress)
                {
                    return;
                }

                _lock.ForWrite(() =>
                {
                    var started = !_scanInProgress && value;
                    var stopped = _scanInProgress && !value;
                    
                    _scanInProgress = value;
                    
                    if (started)
                    {
                        _stopwatch.Restart();
                    }
                    else if (stopped)
                    {
                        _stopwatch.Stop();
                    }
                });
            });

            _ = Task.Run(() => ScanInProgressChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}