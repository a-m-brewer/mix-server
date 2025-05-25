using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class GetFolderNodeQuery
{
    public NodePathRequestDto? NodePath { get; set; }
}