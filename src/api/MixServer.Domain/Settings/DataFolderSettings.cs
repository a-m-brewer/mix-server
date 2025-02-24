namespace MixServer.Domain.Settings;

public class CacheFolderSettings
{
    public string Directory { get; set; } = "./data";
    
    public string TranscodesFolder => Path.Join(Directory, "transcodes");
}