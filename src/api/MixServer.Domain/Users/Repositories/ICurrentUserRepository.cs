using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.Users.Repositories;

public interface ICurrentUserRepository
{
    string CurrentUserId { get; }
    bool HasUserId { get; }
    void SetUserId(string userId);
    Task LoadCurrentPlaybackSessionAsync(CancellationToken cancellationToken);
    Task LoadPlaybackSessionByNodePathAsync(NodePath nodePath, CancellationToken cancellationToken);
    Task LoadPagedPlaybackSessionsAsync(int sessionPageIndex, int sessionPageSize, CancellationToken cancellationToken);
    Task LoadFileSortByAbsolutePathAsync(NodePath nodePath, CancellationToken cancellationToken);
    Task LoadAllDevicesAsync(CancellationToken cancellationToken);
    Task LoadDeviceByIdAsync(Guid deviceId, CancellationToken cancellationToken);
}