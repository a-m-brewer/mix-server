using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Sessions.Validators;

public interface ICanPlayOnDeviceValidator
{
    void ValidateCanPlayOrThrow(IDeviceState deviceState, FileExplorerFileNodeEntity file);
}

public class CanPlayOnDeviceValidator : ICanPlayOnDeviceValidator
{
    public void ValidateCanPlayOrThrow(IDeviceState deviceState, FileExplorerFileNodeEntity file)
    {
        var supportedMimeTypes = deviceState.SupportedMimeTypes;

        if (supportedMimeTypes.Contains(file.MetadataEntity.MimeType) ||
            file.Transcode is { State: TranscodeState.Completed })
        {
            return;
        }
        
        throw new InvalidRequestException(nameof(file.Path), $"{file.Path.AbsolutePath} is not supported for playback on this device");
    }
}