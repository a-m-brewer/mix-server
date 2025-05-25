namespace MixServer.Domain.FileExplorer.Models;

public record NodePath(string RootPath, string RelativePath)
{
    public string AbsolutePath => Path.Join(RootPath, RelativePath);
    public string FileName => Path.GetFileName(AbsolutePath);
    public NodePath Parent => this with { RelativePath = Path.GetDirectoryName(RelativePath) ?? string.Empty };
    
    public string Extension => Path.GetExtension(RelativePath);
    
    public bool IsRoot => string.IsNullOrWhiteSpace(RootPath) && string.IsNullOrWhiteSpace(RelativePath);
    
    public bool IsRootChild => !string.IsNullOrWhiteSpace(RootPath) && string.IsNullOrWhiteSpace(RelativePath);

    public override string ToString()
    {
        return AbsolutePath;
    }

    public bool IsEqualTo(NodePath? other)
    {
        if (other is null)
        {
            return false;
        }
        
        return RootPath == other.RootPath && RelativePath == other.RelativePath;
    }
}