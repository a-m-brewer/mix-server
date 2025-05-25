namespace MixServer.Domain.Settings;

public class CacheFolderSettings
{
    public string Directory { get; set; } = "./data";
    
    public string DirectoryAbsolutePath => Path.GetFullPath(Directory);
    
    public string TranscodesFolder => Path.Join(DirectoryAbsolutePath, "transcodes");
}