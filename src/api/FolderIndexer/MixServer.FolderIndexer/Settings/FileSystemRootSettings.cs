namespace MixServer.FolderIndexer.Settings;

public class FileSystemRootSettings
{
    public string Children { get; set; } = string.Empty;

    public IEnumerable<string> ChildrenSplit => Children.Split(";")
        .Where(w => !string.IsNullOrWhiteSpace(w));
}