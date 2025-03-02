using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Sessions.Validators;

public interface ICanPlayOnDeviceValidator
{
    void ValidateCanPlayOrThrow(IDeviceState deviceState, IFileExplorerFileNode file);
    bool CanPlay(IFileExplorerFileNode itemFile);
    bool CanPlay(IDeviceState deviceState, IFileExplorerFileNode itemFile);
}

public class CanPlayOnDeviceValidator(ITranscodeCache transcodeCache,
    IRequestedPlaybackDeviceAccessor requestedPlaybackDeviceAccessor) : ICanPlayOnDeviceValidator
{
    public void ValidateCanPlayOrThrow(IDeviceState deviceState, IFileExplorerFileNode file)
    {
        if (!deviceState.CanPlay(file) && transcodeCache.GetTranscodeStatus(file.AbsolutePath) != TranscodeState.Completed)
        {
            throw new InvalidRequestException(nameof(file), $"{file.AbsolutePath} is not supported for playback on this device");
        }
    }

    public bool CanPlay(IFileExplorerFileNode itemFile)
    {
        return CanPlay(requestedPlaybackDeviceAccessor.PlaybackDevice, itemFile);
    }

    public bool CanPlay(IDeviceState deviceState, IFileExplorerFileNode file)
    {
        return file.PlaybackSupported && 
               (deviceState.CanPlay(file) ||
                transcodeCache.GetTranscodeStatus(file.AbsolutePath) == TranscodeState.Completed);
    }
}