using CliWrap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.Persistence;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Entities;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Streams.Repositories;

namespace MixServer.Domain.Streams.Services;

public interface ITranscodeService
{
    Task RequestTranscodeAsync(string absoluteFilePath, int bitrate);
}

public class TranscodeService(
    IOptions<DataFolderSettings> dataFolderSettings,
    IOptions<FfmpegSettings> ffmpegSettings,
    ILogger<TranscodeService> logger,
    ITranscodeCache transcodeCache,
    ITranscodeRepository transcodeRepository,
    IUnitOfWork unitOfWork) : ITranscodeService
{
    private const int HlsTime = 4;
    private const int DefaultBitrate = 192;
    
    public async Task RequestTranscodeAsync(string absoluteFilePath, int bitrate)
    {
        var transcode = await transcodeRepository.GetOrAddAsync(absoluteFilePath);

        await unitOfWork.SaveChangesAsync();
        
        var transcodeIdString = transcode.Id.ToString();
        
        Directory.CreateDirectory(GetTranscodeFolder(transcode.Id.ToString()));
        logger.LogDebug("Transcode requested for {AbsoluteFilePath} ({Hash})", absoluteFilePath, transcodeIdString);
        
        _ = Task.Run(() => ProcessTranscode(transcode, bitrate));
    }

    private async Task ProcessTranscode(Transcode transcode, int requestedBitrate)
    {
        var transcodeIdString = transcode.Id.ToString();

        var transcodeFolder = GetTranscodeFolder(transcodeIdString);
        
        var bitrate = requestedBitrate == 0 ? DefaultBitrate : requestedBitrate;
        
        logger.LogInformation("Starting transcode for {AbsoluteFilePath} ({TranscodeId})",
            transcode.AbsolutePath,
            transcodeIdString);
        
        var result = await Cli.Wrap(ffmpegSettings.Value.Path)
            .WithValidation(CommandResultValidation.None)
            .WithArguments([
                "-i", $"{transcode.AbsolutePath}",
                "-c:a", "aac",
                "-b:a", $"{bitrate}k",
                "-vn",
                "-f", "hls",
                "-hls_time", $"{HlsTime}",
                "-hls_list_size", "0",
                "-hls_flags", "append_list+program_date_time+independent_segments",
                "-hls_segment_filename", $"{transcodeIdString}_%06d.ts",
                $"{transcodeIdString}.m3u8"
            ])
            .WithWorkingDirectory(transcodeFolder)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => logger.LogTrace("[{Hash}] {StdOutLine}", transcodeIdString, line)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line => logger.LogDebug("[{Hash}] {StdErrLine}", transcodeIdString, line)))
            .ExecuteAsync();
        
        logger.LogInformation("Transcode {AbsoluteFilePath} ({Hash}) finished after {RunTime} ({StartTime}-{ExitTime}) with exit code {ExitCode}",
            transcode.AbsolutePath,
            transcodeIdString,
            result.RunTime,
            result.StartTime,
            result.ExitTime,
            result.ExitCode);

        if (result.IsSuccess)
        {
            transcodeCache.CalculateHasCompletePlaylist(transcode.Id);
        }
        else
        {
            logger.LogError("Transcode of {AbsoluteFilePath} ({Hash}) failed cleaning up resources", 
                transcode.AbsolutePath,
                transcodeIdString);
            Directory.Delete(transcodeFolder, true);
        }
    }

    private string GetTranscodeFolder(string fileHash)
    {
        return Path.Join(dataFolderSettings.Value.TranscodesFolder, fileHash);
    }
}