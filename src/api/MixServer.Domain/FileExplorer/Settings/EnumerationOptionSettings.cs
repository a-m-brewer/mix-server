namespace MixServer.Domain.FileExplorer.Settings;

public class FileSystemEnumeration
{
    public static readonly EnumerationOptions Options = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = 0
    };
}