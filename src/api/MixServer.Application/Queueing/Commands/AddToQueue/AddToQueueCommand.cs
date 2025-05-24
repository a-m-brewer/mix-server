using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.Queueing.Commands.AddToQueue;

public class AddToQueueCommand
{
    public required NodePathRequestDto NodePath { get; init; }
}