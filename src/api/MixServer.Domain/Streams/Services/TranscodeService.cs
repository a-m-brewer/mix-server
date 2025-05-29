using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.Persistence;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Entities;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Streams.Repositories;

namespace MixServer.Domain.Streams.Services;

public interface ITranscodeService
{
    Task RequestTranscodeAsync(FileExplorerFileNodeEntity file, int bitrate, CancellationToken cancellationToken);
}

public class TranscodeService(
    IOptions<CacheFolderSettings> cacheFolderSettings,
    ILogger<TranscodeService> logger,
    ITranscodeChannel transcodeChannel,
    ITranscodeRepository transcodeRepository,
    IUnitOfWork unitOfWork) : ITranscodeService
{
    public async Task RequestTranscodeAsync(
        FileExplorerFileNodeEntity file,
        int bitrate,
        CancellationToken cancellationToken)
    {
        var transcode = file.Transcode;
        if (transcode is null)
        {
            transcode = new Transcode
            {
                Id = Guid.NewGuid(),
                NodeEntity = file,
                NodeIdEntity = file.Id
            };
            await transcodeRepository.AddAsync(transcode, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        Directory.CreateDirectory(cacheFolderSettings.Value.GetTranscodeFolder(transcode.Id.ToString()));
        logger.LogDebug("Transcode requested for {AbsoluteFilePath} ({Hash})", file.Path.AbsolutePath, transcode.Id);

        _ = transcodeChannel.WriteAsync(new TranscodeRequest(transcode.Id, bitrate), cancellationToken);
    }
}