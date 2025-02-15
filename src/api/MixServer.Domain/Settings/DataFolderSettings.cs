namespace MixServer.Domain.Settings;

public class DataFolderSettings
{
    public string Directory { get; set; } = "./data";
    
    public string TranscodesFolder => Path.Join(Directory, "transcodes");
}