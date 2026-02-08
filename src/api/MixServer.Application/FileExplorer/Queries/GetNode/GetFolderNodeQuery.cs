namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class GetFolderNodeQuery
{
    public string? RootPath { get; set; }
    public string? RelativePath { get; set; }
    public int? StartIndex { get; set; }
    public int? EndIndex { get; set; }
}