using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.Sessions.Commands.SetCurrentSession;

public class SetCurrentSessionCommand
{
    public required NodePathDto NodePath { get; init; }
}