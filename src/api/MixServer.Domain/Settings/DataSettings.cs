namespace MixServer.Domain.Settings;

public class DataSettings
{
    public string DataDir { get; set; } = "./data";
    
    public string AbsoluteDataDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDir);
}