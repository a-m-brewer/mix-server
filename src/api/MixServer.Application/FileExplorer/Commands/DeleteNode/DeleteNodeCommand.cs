using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.FileExplorer.Commands.DeleteNode;

public class DeleteNodeCommand
{
    public required NodePathDto NodePath { get; init; }
}