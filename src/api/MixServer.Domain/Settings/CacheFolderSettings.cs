namespace MixServer.Domain.Settings;

public class CacheFolderSettings
{
    public string Directory { get; set; } = "./data";
    
    public string DirectoryAbsolutePath => Path.GetFullPath(Directory);
    
    public string TranscodesFolder => Path.Join(DirectoryAbsolutePath, "transcodes");
    
    public int TranscodeWorkers { get; set; } = 4;
    
    public string GetTranscodeFolder(string fileHash)
    {
        return Path.Join(TranscodesFolder, fileHash);
    }
}