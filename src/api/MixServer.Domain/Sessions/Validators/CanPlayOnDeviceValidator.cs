using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Sessions.Validators;

public interface ICanPlayOnDeviceValidator
{
    void ValidateCanPlayOrThrow(IDeviceState deviceState, NodePath nodePath, string? mimeType);
    Task<bool> CanPlayAsync(IFileExplorerFileNode itemFile);
}

public class CanPlayOnDeviceValidator(ITranscodeCache transcodeCache,
    IRequestedPlaybackDeviceAccessor requestedPlaybackDeviceAccessor) : ICanPlayOnDeviceValidator
{
    public void ValidateCanPlayOrThrow(IDeviceState deviceState, NodePath nodePath, string? mimeType)
    {
        if (!deviceState.GetMimeTypeSupported(mimeType) &&
            transcodeCache.GetTranscodeStatus(nodePath) != TranscodeState.Completed)
        {
            throw new InvalidRequestException(nameof(nodePath), $"{nodePath.AbsolutePath} is not supported for playback on this device");
        }
    }

    public async Task<bool> CanPlayAsync(IFileExplorerFileNode itemFile)
    {
        return CanPlay(await requestedPlaybackDeviceAccessor.GetPlaybackDeviceAsync(), itemFile);
    }

    private bool CanPlay(IDeviceState deviceState, IFileExplorerFileNode file)
    {
        return file.PlaybackSupported && 
               (deviceState.GetMimeTypeSupported(file.Metadata.MimeType) ||
                transcodeCache.GetTranscodeStatus(file.Path) == TranscodeState.Completed);
    }
}