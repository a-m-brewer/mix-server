using CliWrap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Streams.Repositories;

namespace MixServer.Application.Streams.Commands.ProcessTranscode;

public class ProcessTranscodeCommandHandler(
    IOptions<CacheFolderSettings> cacheFolderSettings,
    IOptions<FfmpegSettings> ffmpegSettings,
    ILogger<ProcessTranscodeCommandHandler> logger,
    ITranscodeCache transcodeCache,
    ITranscodeRepository transcodeRepository,
    IOptions<TranscodeSettings> transcodeSettings) : ICommandHandler<TranscodeRequest>
{
    public async Task HandleAsync(TranscodeRequest request, CancellationToken cancellationToken)
    {
        var transcode = await transcodeRepository.GetAsync(request.TranscodeId, cancellationToken);
        
        var transcodeIdString = transcode.Id.ToString();

        var transcodeFolder = cacheFolderSettings.Value.GetTranscodeFolder(transcodeIdString);
        
        var bitrate = request.Bitrate == 0
            ? transcodeSettings.Value.DefaultBitrate
            : request.Bitrate;
        var hlsTime = transcodeSettings.Value.HlsTimeInSeconds;
        
        logger.LogInformation("Starting transcode for {AbsoluteFilePath} ({TranscodeId})",
            transcode.NodeEntity.Path.AbsolutePath,
            transcodeIdString);
        
        var result = await Cli.Wrap(ffmpegSettings.Value.Path)
            .WithValidation(CommandResultValidation.None)
            .WithArguments([
                "-i", $"{transcode.NodeEntity.Path.AbsolutePath}",
                "-c:a", "aac",
                "-b:a", $"{bitrate}k",
                "-vn",
                "-f", "hls",
                "-hls_time", $"{hlsTime}",
                "-hls_list_size", "0",
                "-hls_flags", "append_list+program_date_time+independent_segments",
                "-hls_segment_filename", $"{transcodeIdString}_%06d.ts",
                $"{transcodeIdString}.m3u8"
            ])
            .WithWorkingDirectory(transcodeFolder)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => logger.LogTrace("[{Hash}] {StdOutLine}", transcodeIdString, line)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line => logger.LogDebug("[{Hash}] {StdErrLine}", transcodeIdString, line)))
            .ExecuteAsync(cancellationToken);
        
        logger.LogInformation("Transcode {AbsoluteFilePath} ({Hash}) finished after {RunTime} ({StartTime}-{ExitTime}) with exit code {ExitCode}",
            transcode.NodeEntity.Path.AbsolutePath,
            transcodeIdString,
            result.RunTime,
            result.StartTime,
            result.ExitTime,
            result.ExitCode);

        if (result.IsSuccess)
        {
            transcodeCache.CalculateHasCompletePlaylist(transcode.Id, cancellationToken);
        }
        else
        {
            logger.LogError("Transcode of {AbsoluteFilePath} ({Hash}) failed cleaning up resources", 
                transcode.NodeEntity.Path.AbsolutePath,
                transcodeIdString);

            try
            {
                Directory.Delete(transcodeFolder, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete transcode folder {TranscodeFolder}", transcodeFolder);
            }
        }
    }
}