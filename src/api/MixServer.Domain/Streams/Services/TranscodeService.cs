using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Extensions;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Streams.Repositories;
using MixServer.Domain.Utilities;

namespace MixServer.Domain.Streams.Services;

public interface ITranscodeService
{
    Task<PlaylistFileInfo> StartTranscodeAsync(string absoluteFilePath, int? requestedBitrate);

    Task<SegmentFileInfo> GetSegmentAsync(string segment);
}

public class TranscodeService(
    IOptions<DataSettings> dataSettings,
    IHashService hashService,
    ILogger<TranscodeService> logger,
    IProcessService processService,
    ITranscodeRepository transcodeRepository) : ITranscodeService
{
    private const int HlsTime = 60;
    private const int DefaultBitrate = 192;

    public async Task<PlaylistFileInfo> StartTranscodeAsync(string absoluteFilePath, int? requestedBitrate)
    {
        var hash = hashService.GetHash(absoluteFilePath);
        logger.LogInformation("Starting transcode for {AbsoluteFilePath}", absoluteFilePath);
        
        transcodeRepository.AddTranscode(hash, new TranscodeInfo(absoluteFilePath, requestedBitrate));
        
        var transcodeFolder = GetOrCreateTranscodeFolder(hash);

        var playlistName = $"{hash}.m3u8";
        var playlistPath = Path.Join(transcodeFolder, playlistName);

        var ffmpegArgs = CreateFullTranscodeArguments(
            absoluteFilePath,
            hash,
            requestedBitrate);
        
        if (File.Exists(playlistPath))
        {
            logger.LogInformation("Transcode already exists for {AbsoluteFilePath}", absoluteFilePath);
            return new PlaylistFileInfo
            {
                Path = playlistPath
            };
        }

        if (processService.ProcessExists(hash))
        {
            logger.LogInformation("Transcode already in progress for {AbsoluteFilePath}", absoluteFilePath);
            
            await AsyncFileSystemWatcher.WaitForFileAsync(transcodeFolder, playlistName, TimeSpan.FromMinutes(2));
            
            logger.LogInformation("(existing process) Found transcode playlist for {AbsoluteFilePath}", absoluteFilePath);
            return new PlaylistFileInfo
            {
                Path = playlistPath
            };
        }
        
        logger.LogInformation("Starting transcode process for {AbsoluteFilePath}", absoluteFilePath);
        processService.StartProcess(hash, @"C:\Users\avoid\AppData\Local\Microsoft\WinGet\Links\ffmpeg.exe", ffmpegArgs, new ProcessSettings
        {
            WorkingDirectory = transcodeFolder,
            StdErrLogLevel = LogLevel.Information,
            StdOutLogLevel = LogLevel.Debug,
            OnExit = () =>
            {
                transcodeRepository.RemoveTranscode(hash);
            }
        });

        await AsyncFileSystemWatcher.WaitForFileAsync(transcodeFolder, playlistName, TimeSpan.FromMinutes(2));

        logger.LogInformation("(fresh playlist) Transcode completed for {AbsoluteFilePath}", absoluteFilePath);
        return new PlaylistFileInfo
        {
            Path = playlistPath
        };
    }

    public async Task<SegmentFileInfo> GetSegmentAsync(string segment)
    {
        var splitSegment = segment.Split("_");
        var hash = splitSegment[0];

        var transcodeFolder = GetTranscodeFolderOrThrow(hash);
        
        var segmentPath = Path.Join(transcodeFolder, segment);

        if (!File.Exists(segmentPath))
        {
            await AsyncFileSystemWatcher.WaitForFileAsync(transcodeFolder, segment, TimeSpan.FromMinutes(2));
        }
        
        return new SegmentFileInfo
        {
            Path = segmentPath
        };
    }

    private string GetOrCreateTranscodeFolder(string hash)
    {
        var transcodeFolder = GetTranscodeFolder(hash);

        if (!Directory.Exists(transcodeFolder))
        {
            Directory.CreateDirectory(transcodeFolder);
        }
        
        return transcodeFolder;
    }

    private string GetTranscodeFolderOrThrow(string hash)
    {
        var transcodeFolder = GetTranscodeFolder(hash);

        if (!Directory.Exists(transcodeFolder))
        {
            throw new NotFoundException("transcodes.hls", hash);
        }
        
        return transcodeFolder;
    }
    
    private string GetTranscodeFolder(string hash) => Path.Combine(dataSettings.Value.AbsoluteDataDir, "transcodes", hash, "hls");

    private static string CreateFullTranscodeArguments(
        string absoluteFilePath,
        string hash,
        int? bitrate)
    {
        var args = new[]
        {
            "-i", $"\"{absoluteFilePath}\"",
            "-c:a", "aac",
            "-b:a", $"{bitrate ?? DefaultBitrate}k",
            "-vn",
            "-f", "hls",
            "-hls_time", $"{HlsTime}",
            "-hls_list_size", "0",
            "-hls_flags", "append_list+program_date_time+independent_segments",
            "-hls_segment_filename", $"{hash}_%03d.ts",
            $"\"{hash}.m3u8\""
        };
        
        return string.Join(" ", args);
    }


}