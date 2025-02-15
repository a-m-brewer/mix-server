using Microsoft.Extensions.Options;
using MixServer.Domain.Settings;

namespace MixServer.Domain.Streams.Services;

public interface ITranscodeService
{
    void RequestTranscode(string absoluteFilePath, string fileHash);
}

public class TranscodeService(IOptions<DataFolderSettings> dataFolderSettings) : ITranscodeService
{
    public void RequestTranscode(string absoluteFilePath, string fileHash)
    {
        Directory.CreateDirectory(Path.Join(dataFolderSettings.Value.TranscodesFolder, fileHash));
    }
}