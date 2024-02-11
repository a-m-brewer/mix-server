namespace MixServer.Domain.FileExplorer.Settings;

public class RootFolderSettings
{
    public string Children { get; set; } = string.Empty;

    public IEnumerable<string> ChildrenSplit => Children.Split(";")
        .Where(w => !string.IsNullOrWhiteSpace(w));
}