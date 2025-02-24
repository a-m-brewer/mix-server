namespace MixServer.Domain.FileExplorer.Models;

public record NodePath(string ParentAbsolutePath, string FileName)
{
    public string AbsolutePath => Path.Join(ParentAbsolutePath, FileName);
}