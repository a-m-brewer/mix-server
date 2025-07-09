using MixServer.Domain.Persistence;
using MixServer.Domain.Utilities;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IFolderScanTrackingStore : ISingletonRepository
{
    event EventHandler ScanInProgressChanged;
    bool ScanInProgress { get; set; }
}

public class FolderScanTrackingStore : IFolderScanTrackingStore
{
    private readonly ReadWriteLock _lock = new();
    private bool _scanInProgress;

    public event EventHandler? ScanInProgressChanged;

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
                    _scanInProgress = value;
                });
            });

            _ = Task.Run(() => ScanInProgressChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}