using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.FileExplorer.Commands.DeleteNode;

public class DeleteNodeCommand
{
    public required NodePathRequestDto NodePath { get; init; }
}