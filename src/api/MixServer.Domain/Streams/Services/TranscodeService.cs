using CliWrap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Caches;

namespace MixServer.Domain.Streams.Services;

public interface ITranscodeService
{
    Task RequestTranscodeAsync(string absoluteFilePath, IMediaMetadata metadata);
}

public class TranscodeService(
    IOptions<DataFolderSettings> dataFolderSettings,
    ILogger<TranscodeService> logger,
    ITranscodeCache transcodeCache) : ITranscodeService
{
    private const int HlsTime = 4;
    private const int DefaultBitrate = 192;
    
    public async Task RequestTranscodeAsync(string absoluteFilePath, IMediaMetadata metadata)
    {
        await transcodeCache.AddTranscodeMappingAsync(metadata.FileHash, absoluteFilePath);
        
        Directory.CreateDirectory(GetTranscodeFolder(metadata.FileHash));
        logger.LogDebug("Transcode requested for {AbsoluteFilePath} ({Hash})", absoluteFilePath, metadata.FileHash);
        
        Task.Run(() => ProcessTranscode(absoluteFilePath, metadata));
    }

    private async Task ProcessTranscode(string absoluteFilePath, IMediaMetadata metadata)
    {
        var transcodeFolder = GetTranscodeFolder(metadata.FileHash);
        
        var bitrate = metadata.Bitrate == 0 ? DefaultBitrate : metadata.Bitrate;
        
        logger.LogInformation("Starting transcode for {AbsoluteFilePath} ({Hash})",
            absoluteFilePath,
            metadata.FileHash);
        
        var result = await Cli.Wrap("ffmpeg")
            .WithValidation(CommandResultValidation.None)
            .WithArguments([
                "-i", $"{absoluteFilePath}",
                "-c:a", "aac",
                "-b:a", $"{bitrate}k",
                "-vn",
                "-f", "hls",
                "-hls_time", $"{HlsTime}",
                "-hls_list_size", "0",
                "-hls_flags", "append_list+program_date_time+independent_segments",
                "-hls_segment_filename", $"{metadata.FileHash}_%06d.ts",
                $"{metadata.FileHash}.m3u8"
            ])
            .WithWorkingDirectory(transcodeFolder)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => logger.LogTrace("[{Hash}] {StdOutLine}", metadata.FileHash, line)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line => logger.LogDebug("[{Hash}] {StdErrLine}", metadata.FileHash, line)))
            .ExecuteAsync();
        
        logger.LogInformation("Transcode {AbsoluteFilePath} ({Hash}) finished after {RunTime} ({StartTime}-{ExitTime}) with exit code {ExitCode}",
            absoluteFilePath,
            metadata.FileHash,
            result.RunTime,
            result.StartTime,
            result.ExitTime,
            result.ExitCode);

        if (result.IsSuccess)
        {
            transcodeCache.CalculateHasCompletePlaylist(metadata.FileHash);
        }
        else
        {
            logger.LogError("Transcode of {AbsoluteFilePath} ({Hash}) failed cleaning up resources", 
                absoluteFilePath,
                metadata.FileHash);
            Directory.Delete(transcodeFolder, true);
        }
    }

    private string GetTranscodeFolder(string fileHash)
    {
        return Path.Join(dataFolderSettings.Value.TranscodesFolder, fileHash);
    }
}